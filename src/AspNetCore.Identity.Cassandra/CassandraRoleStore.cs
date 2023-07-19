using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.Identity.Cassandra.Extensions;
using AspNetCore.Identity.Cassandra.Models;
using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetCore.Identity.Cassandra;

public class CassandraRoleStore<TRole, TSession> : 
    IQueryableRoleStore<TRole>,
    IRoleClaimStore<TRole>
    where TRole : CassandraIdentityRole
    where TSession : class, ISession
{
    #region | Fields

    private readonly IMapper _mapper;
    private readonly Table<TRole> _table;
    private bool _isDisposed;
    private readonly CqlQueryOptions _cqlQueryOptions;
    private readonly CassandraOptions _cassandraOptions;
    private readonly ILogger<CassandraRoleStore<TRole, TSession>> _logger;

    #endregion

    #region | Properties

    public IdentityErrorDescriber ErrorDescriber { get; }
    public CassandraErrorDescriber CassandraErrorDescriber { get; }
    public TSession Session { get; }
    public IQueryable<TRole> Roles => _table;

    #endregion

    #region | Constructors

    public CassandraRoleStore(
        TSession session,
        CqlQueryOptions cqlQueryOptions,
        IOptions<CassandraOptions> cassandraOptions, 
        IdentityErrorDescriber errorDescriber,
        CassandraErrorDescriber cassandraErrorDescriber,
        ILogger<CassandraRoleStore<TRole, TSession>> logger)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(errorDescriber);
        ArgumentNullException.ThrowIfNull(cassandraErrorDescriber);

        Session = session;
        ErrorDescriber = errorDescriber;
        CassandraErrorDescriber = cassandraErrorDescriber;

        _mapper = new Mapper(session);
        _table = new Table<TRole>(session);
        _cqlQueryOptions = cqlQueryOptions;
        _cassandraOptions = cassandraOptions.Value;
        _cqlQueryOptions = cqlQueryOptions;
        _logger = logger;
    }

    #endregion

    #region | Public Methods

    public Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        return _mapper.TryInsertAsync(role, _cqlQueryOptions, CassandraErrorDescriber, _logger);
    }

    public async Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(role);

        var originalRole = await FindByIdAsync(role.Id.ToString(), cancellationToken);
        var affectedUsers = (await _mapper.FetchAsync<Guid>(
            $"SELECT id FROM {_cassandraOptions.KeyspaceName}.{CassandraSessionHelper.UsersTableName} WHERE roles CONTAINS ?",
            originalRole?.NormalizedName)).ToList();

        // Role cannot be changed directly in a list. We have to remove it first, and then add it again
        return await _mapper.TryExecuteBatchAsync(CassandraErrorDescriber, _logger, _cassandraOptions.Query,
            batch =>
            {
                // Remove original role from existing users
                if (!affectedUsers.Any())
                    return;

                batch.Execute(
                    $"UPDATE {_cassandraOptions.KeyspaceName}.{CassandraSessionHelper.UsersTableName} SET roles = roles - ['{originalRole.NormalizedName}'] WHERE Id IN ?",
                    affectedUsers);
            },
            batch =>
            {
                // Add updated role to existing users
                if (!affectedUsers.Any())
                    return;

                batch.Execute(
                    $"UPDATE {_cassandraOptions.KeyspaceName}.{CassandraSessionHelper.UsersTableName} SET roles = roles + ['{role.NormalizedName}'] WHERE Id IN ?",
                    affectedUsers);
            },
            batch => batch.Update(role));
    }

    public async Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(role);

        var affectedUsers = (await _mapper.FetchAsync<Guid>(
            $"SELECT id FROM {_cassandraOptions.KeyspaceName}.{CassandraSessionHelper.UsersTableName} WHERE roles CONTAINS ?",
            role.NormalizedName)).ToList();

        return await _mapper.TryExecuteBatchAsync(CassandraErrorDescriber, _logger, _cassandraOptions.Query,
            batch =>
            {
                if (!affectedUsers.Any())
                    return;

                batch.Execute(
                    $"UPDATE {_cassandraOptions.KeyspaceName}.{CassandraSessionHelper.UsersTableName} SET roles = roles - ['{role.NormalizedName}'] WHERE Id IN ?",
                    affectedUsers);
            },
            batch => batch.Delete(role));
    }

    public Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(role);

        return Task.FromResult(role.Id.ToString());
    }

    public Task<string?> GetRoleNameAsync(TRole role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(role);

        return Task.FromResult(role.Name);
    }

    public Task SetRoleNameAsync(TRole role, string? roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(role);

        role.Name = roleName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedRoleNameAsync(TRole role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(role);

        return Task.FromResult(role.NormalizedName);
    }

    public Task SetNormalizedRoleNameAsync(TRole role, string? normalizedName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(role);

        role.NormalizedName = normalizedName;
        return Task.CompletedTask;
    }

    public Task<TRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        return _mapper.SingleOrDefaultAsync<TRole?>("WHERE Id = ?", Guid.Parse(roleId));
    }

    public Task<TRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        return _mapper.SingleOrDefaultAsync<TRole?>("FROM roles_by_name WHERE NormalizedName = ?", normalizedRoleName);
    }

    public async Task<IList<Claim>> GetClaimsAsync(TRole role, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(role);

        var ps = await Session.PrepareAsync($"SELECT * FROM {_cassandraOptions.KeyspaceName}.roleclaims WHERE roleid = ?");
        var statement = ps.Bind(role.Id);

        var rs = await Session.ExecuteAsync(statement);
        return rs
            .Select(x => new Claim(x.GetValue<string>("type"), x.GetValue<string>("value")))
            .ToList();
    }

    public async Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(claim);

        var ps = await Session.PrepareAsync($"INSERT INTO {_cassandraOptions.KeyspaceName}.roleclaims(roleid, type, value) VALUES(?, ?, ?)");
        var statement = ps.Bind(role.Id, claim.Type, claim.Value);

        await Session.ExecuteAsync(statement);
    }

    public async Task RemoveClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(claim);

        var ps = await Session.PrepareAsync($"DELETE FROM {_cassandraOptions.KeyspaceName}.roleclaims WHERE roleid = ? AND type = ? AND value = ?");
        var statement = ps.Bind(role.Id, claim.Type, claim.Value);

        await Session.ExecuteAsync(statement);
    }

    #endregion

    #region | IDisposable

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(GetType().Name);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;
            
        _isDisposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}