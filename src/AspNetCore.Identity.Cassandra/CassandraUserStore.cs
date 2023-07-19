﻿using AspNetCore.Identity.Cassandra.Extensions;
using AspNetCore.Identity.Cassandra.Models;
using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.Identity.Cassandra;

public class CassandraUserStore<TUser, TSession> :
    IUserLoginStore<TUser>,
    IUserPasswordStore<TUser>,
    IUserSecurityStampStore<TUser>,
    IUserPhoneNumberStore<TUser>,
    IUserEmailStore<TUser>,
    IUserRoleStore<TUser>,
    IQueryableUserStore<TUser>,
    IUserTwoFactorStore<TUser>,
    IUserAuthenticatorKeyStore<TUser>,
    IUserTwoFactorRecoveryCodeStore<TUser>,
    IUserLockoutStore<TUser>,
    IUserClaimStore<TUser>
    where TUser : CassandraIdentityUser
    where TSession : class, ISession
{
    #region | Fields

    private readonly IMapper _mapper;
    private readonly Table<TUser> _usersTable;
    private readonly CqlQueryOptions _cqlQueryOptions;
    private readonly CassandraOptions _cassandraOptions;
    private readonly ILogger<CassandraUserStore<TUser, TSession>> _logger;
    private bool _isDisposed;

    private const string InternalLoginProvider = "[CassandraUserStore]";
    private const string AuthenticatorKeyTokenName = "AuthenticatorKey";
    private const string RecoveryCodeTokenName = "RecoveryCodes";

    #endregion

    #region | Properties

    public IdentityErrorDescriber ErrorDescriber { get; }
    public CassandraErrorDescriber CassandraErrorDescriber { get; }
    public TSession Session { get; }
    public IQueryable<TUser> Users => _usersTable;

    #endregion

    #region | Constructors

    public CassandraUserStore(
        TSession session,
        CqlQueryOptions cqlQueryOptions,
        IOptions<CassandraOptions> cassandraOptions,
        IdentityErrorDescriber errorDescriber,
        CassandraErrorDescriber cassandraErrorDescriber,
        ILogger<CassandraUserStore<TUser, TSession>> logger)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(errorDescriber);
        ArgumentNullException.ThrowIfNull(cassandraErrorDescriber);

        Session = session;
        ErrorDescriber = errorDescriber;
        CassandraErrorDescriber = cassandraErrorDescriber;

        _mapper = new Mapper(session);
        _usersTable = new Table<TUser>(session);
        _cassandraOptions = cassandraOptions.Value;
        _cqlQueryOptions = cqlQueryOptions;
        _logger = logger;
    }

    #endregion

    #region | Public Methods

    #region | Stores

    #region | IUserLoginStore

    public Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(login);

        user.AddLogin(login);
        return Task.CompletedTask;
    }

    public Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        user.RemoveLogin(loginProvider, providerKey);
        return Task.CompletedTask;
    }

    public Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult<IList<UserLoginInfo>>(user.Logins.Select(x => new UserLoginInfo(x.LoginProvider, x.ProviderKey, x.ProviderDisplayName)).ToList());
    }

    public Task<TUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(loginProvider);
        ArgumentNullException.ThrowIfNull(providerKey);

        return _mapper.SingleOrDefaultAsync<TUser?>(
            "WHERE logins CONTAINS {loginprovider: ?, providerkey: ?, providerdisplayname: ?} ALLOW FILTERING",
            loginProvider, providerKey, loginProvider);
    }

    #endregion

    #region | IUserPasswordStore

    public Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.Id.ToString());
    }

    public Task<string?> GetUserNameAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.UserName);
    }

    public Task SetUserNameAsync(TUser user, string? userName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(userName);

        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.NormalizedUserName);
    }

    public Task SetNormalizedUserNameAsync(TUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return _mapper.TryInsertAsync(user, _cqlQueryOptions, CassandraErrorDescriber, _logger);
    }

    public Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        user.CleanUp();
        return _mapper.TryUpdateAsync(user, _cqlQueryOptions, CassandraErrorDescriber, _logger);
    }

    public Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return _mapper.TryDeleteAsync(user, _cqlQueryOptions, CassandraErrorDescriber, _logger);
    }

    public Task<TUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        cancellationToken.ThrowIfCancellationRequested();
        return _mapper.SingleOrDefaultAsync<TUser?>("WHERE id = ?", Guid.Parse(userId));
    }

    public Task<TUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        cancellationToken.ThrowIfCancellationRequested();
        return _mapper.SingleOrDefaultAsync<TUser?>("FROM users_by_username WHERE NormalizedUserName = ?", normalizedUserName);
    }

    public Task SetPasswordHashAsync(TUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.PasswordHash);
    }

    public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(string.IsNullOrEmpty(user.PasswordHash) is false);
    }

    #endregion

    #region | IUserSecurityStampStore

    public Task SetSecurityStampAsync(TUser user, string stamp, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task<string?> GetSecurityStampAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.SecurityStamp);
    }

    #endregion

    #region | IUserPhoneNumberStore

    public Task SetPhoneNumberAsync(TUser user, string? phoneNumber, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        user.Phone = phoneNumber;
        return Task.CompletedTask;
    }

    public Task<string?> GetPhoneNumberAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.Phone?.Number);
    }

    public Task<bool> GetPhoneNumberConfirmedAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        if (user.Phone is null)
            throw new InvalidOperationException("Cannot get the confirmation status of the phone number since the user doesn't have a phone number.");

        return Task.FromResult(user.Phone.IsConfirmed);
    }

    public Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        if (user.Phone is null)
            throw new InvalidOperationException("Cannot set the confirmation status of the phone number since the user doesn't have a phone number.");

        user.Phone.ConfirmationTime = confirmed
            ? DateTimeOffset.UtcNow
            : null;

        return Task.CompletedTask;
    }

    #endregion

    #region | IUserEmailStore

    public Task SetEmailAsync(TUser user, string? email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.Email);
    }

    public Task<bool> GetEmailConfirmedAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.EmailConfirmed);
    }

    public Task SetEmailConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        user.EmailConfirmationTime = confirmed
            ? DateTimeOffset.UtcNow
            : null;

        return Task.CompletedTask;
    }

    public Task<TUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        return _mapper.SingleOrDefaultAsync<TUser?>("FROM users_by_email WHERE NormalizedEmail = ?", normalizedEmail);
    }

    public Task<string?> GetNormalizedEmailAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.NormalizedEmail);
    }

    public Task SetNormalizedEmailAsync(TUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    #endregion

    #region | IUserRoleStore

    public Task AddToRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        user.AddRole(roleName);
        return Task.CompletedTask;
    }

    public Task RemoveFromRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        user.RemoveRole(roleName);
        return Task.CompletedTask;
    }

    public Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult<IList<string>>(user.Roles.ToList());
    }

    public Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.Roles.Contains(roleName));
    }

    public async Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        return (await _mapper.FetchAsync<TUser>("WHERE roles CONTAINS ?", roleName)).ToList();
    }

    #endregion

    #region | IUserTwoFactorStore

    public Task SetTwoFactorEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        user.TwoFactorEnabled = enabled;
        return Task.CompletedTask;
    }

    public Task<bool> GetTwoFactorEnabledAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.TwoFactorEnabled);
    }

    #endregion

    #region | IUserAuthenticatorKeyStore

    public Task SetAuthenticatorKeyAsync(TUser user, string key, CancellationToken cancellationToken)
        => SetTokenAsync(user, InternalLoginProvider, AuthenticatorKeyTokenName, key, cancellationToken);

    public Task<string?> GetAuthenticatorKeyAsync(TUser user, CancellationToken cancellationToken)
        => GetTokenAsync(user, InternalLoginProvider, AuthenticatorKeyTokenName, cancellationToken);

    #endregion

    #region | IUserTwoFactorRecoveryCodeStore

    public Task ReplaceCodesAsync(TUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken)
    {
        var mergedCodes = string.Join(";", recoveryCodes);
        return SetTokenAsync(user, InternalLoginProvider, RecoveryCodeTokenName, mergedCodes, cancellationToken);
    }

    public async Task<bool> RedeemCodeAsync(TUser user, string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(code);

        var mergedCodes = await GetTokenAsync(user, InternalLoginProvider, RecoveryCodeTokenName, cancellationToken) ?? "";
        var splitCodes = mergedCodes.Split(';');
        if (splitCodes.Contains(code) is false)
            return false;

        var updatedCodes = splitCodes.Where(s => s != code).ToList();
        await ReplaceCodesAsync(user, updatedCodes, cancellationToken);
        return true;
    }

    public async Task<int> CountCodesAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        var mergedCodes = await GetTokenAsync(user, InternalLoginProvider, RecoveryCodeTokenName, cancellationToken) ?? "";
        return mergedCodes.Length > 0
            ? mergedCodes.Split(';').Length
            : 0;
    }

    #endregion

    #region | IUserLockoutStore

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.Lockout?.EndDate);
    }

    public Task SetLockoutEndDateAsync(TUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        user.Lockout ??= new LockoutInfo();

        user.Lockout.EndDate = lockoutEnd;
        return Task.CompletedTask;
    }

    public Task<int> IncrementAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        user.Lockout ??= new LockoutInfo();

        var newAccessFailedCount = ++user.Lockout.AccessFailedCount;
        return Task.FromResult(newAccessFailedCount);
    }

    public Task ResetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        if (user.Lockout is not null)
            user.Lockout.AccessFailedCount = 0;

        return Task.CompletedTask;
    }

    public Task<int> GetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.Lockout?.AccessFailedCount ?? 0);
    }

    public Task<bool> GetLockoutEnabledAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.Lockout is { Enabled: true });
    }

    public Task SetLockoutEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        user.Lockout ??= new LockoutInfo();

        user.Lockout.Enabled = enabled;
        return Task.CompletedTask;
    }

    #endregion

    #region | IUserClaimStore

    public async Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        var ps = await Session.PrepareAsync($"SELECT * FROM {_cassandraOptions.KeyspaceName}.userclaims WHERE userid = ?");
        var statement = ps.Bind(user.Id);

        var rs = await Session.ExecuteAsync(statement);
        return rs
            .Select(x => new Claim(x.GetValue<string>("type"), x.GetValue<string>("value")))
            .ToList();
    }

    public async Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);
        
        var batch = _mapper
            .CreateBatch()
            .WithOptions(_cassandraOptions.Query);

        foreach (var claim in claims)
        {
            batch.Execute($"INSERT INTO {_cassandraOptions.KeyspaceName}.userclaims(userid, type, value) VALUES(?, ?, ?)",
                user.Id, claim.Type, claim.Value);
        }

        await _mapper.ExecuteAsync(batch);
    }

    public async Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        var batch = _mapper
            .CreateBatch()
            .WithOptions(_cassandraOptions.Query);

        batch.Execute($"DELETE FROM {_cassandraOptions.KeyspaceName}.userclaims WHERE userid = ? AND type = ? AND value = ?",
            user.Id, claim.Type, claim.Value);

        batch.Execute($"INSERT INTO {_cassandraOptions.KeyspaceName}.userclaims(userid, type, value) VALUES(?, ?, ?)",
            user.Id, newClaim.Type, newClaim.Value);

        await _mapper.ExecuteAsync(batch);
    }

    public async Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);
        
        var batch = _mapper
            .CreateBatch()
            .WithOptions(_cassandraOptions.Query);

        foreach (var claim in claims)
        {
            batch.Execute($"DELETE FROM {_cassandraOptions.KeyspaceName}.userclaims WHERE userid = ? AND type = ? AND value = ?",
                user.Id, claim.Type, claim.Value);
        }

        await _mapper.ExecuteAsync(batch);
    }

    public async Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
    {
        var affectedUsers = (await _mapper.FetchAsync<Guid>(
            "SELECT userid FROM userclaims_by_type_and_value WHERE type = ? AND value = ?",
            claim.Type, claim.Value)).ToList();

        return (await _mapper.FetchAsync<TUser>("WHERE id IN ?", affectedUsers)).ToList();
    }

    #endregion

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

    #endregion

    #region | Private Methods

    private Task SetTokenAsync(TUser user, string loginProvider, string name, string value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        user.RemoveToken(loginProvider, name);
        user.AddToken(new TokenInfo(loginProvider, name, value));
        return Task.CompletedTask;
    }

    private Task<string?> GetTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        var token = user.Tokens.SingleOrDefault(x => x.LoginProvider == loginProvider && x.Name == name);
        return Task.FromResult(token?.Value);
    }

    #endregion
}