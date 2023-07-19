using System;
using AspNetCore.Identity.Cassandra.Models;
using Cassandra;
using Cassandra.Data.Linq;
using Microsoft.Extensions.Options;

namespace AspNetCore.Identity.Cassandra;

public class DbInitializer
{
    private readonly ISession _session;
    private readonly CassandraOptions _options;

    public DbInitializer(ISession session, IOptions<CassandraOptions> options)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(options);

        _session = session;
        _options = options.Value;
    }

    public void Initialize<TUser, TRole>()
    {
        InitializeKeyspace();
        InitializeUserDefinedTypes();

        var usersTable = InitializeTable<TUser>();
        var rolesTable = InitializeTable<TRole>();
        CassandraSessionHelper.UsersTableName = usersTable.GetTable().Name;
        CassandraSessionHelper.RolesTableName = rolesTable.GetTable().Name;

        InitializeTableUserClaims();
        InitializeTableRoleClaims();
        InitializeMaterializedViews();
    }

    public void InitializeKeyspace()
    {
        ArgumentException.ThrowIfNullOrEmpty(_options.KeyspaceName);

        try
        {
            // Attempt to switch to keyspace
            _session.ChangeKeyspace(_options.KeyspaceName);
        }
        catch (InvalidQueryException)
        {
            // If failed with InvalidQueryException then keyspace does not exist
            // -> create new one
            _session.CreateKeyspaceIfNotExists(_options.KeyspaceName, replication: _options.Replication, durableWrites: _options.DurableWrites);
            _session.ChangeKeyspace(_options.KeyspaceName);
        }
    }

    public void InitializeUserDefinedTypes()
    {
        _session.Execute($"CREATE TYPE IF NOT EXISTS {_options.KeyspaceName}.LockoutInfo (EndDate timestamp, Enabled boolean, AccessFailedCount int);");
        _session.Execute($"CREATE TYPE IF NOT EXISTS {_options.KeyspaceName}.PhoneInfo (Number text, ConfirmationTime timestamp);");
        _session.Execute($"CREATE TYPE IF NOT EXISTS {_options.KeyspaceName}.LoginInfo (LoginProvider text, ProviderKey text, ProviderDisplayName text);");
        _session.Execute($"CREATE TYPE IF NOT EXISTS {_options.KeyspaceName}.TokenInfo (LoginProvider text, Name text, Value text);");

        _session.UserDefinedTypes.Define(
            UdtMap.For<LockoutInfo>(),
            UdtMap.For<PhoneInfo>(),
            UdtMap.For<LoginInfo>(),
            UdtMap.For<TokenInfo>());
    }

    public Table<TTableModel> InitializeTable<TTableModel>()
    {
        var table = new Table<TTableModel>(_session);
        table.CreateIfNotExists();
        return table;
    }

    public void InitializeTableUserClaims() =>
        _session.Execute($"CREATE TABLE IF NOT EXISTS {_options.KeyspaceName}.userclaims (" +
                         " UserId uuid, " +
                         " Type text, " +
                         " Value text, " +
                         " PRIMARY KEY (UserId, Type, Value));");

    public void InitializeTableRoleClaims() =>
        _session.Execute($"CREATE TABLE IF NOT EXISTS {_options.KeyspaceName}.roleclaims (" +
                         " RoleId uuid, " +
                         " Type text, " +
                         " Value text, " +
                         " PRIMARY KEY (RoleId, Type, Value));");

    public void InitializeMaterializedViews()
    {
        _session.Execute("CREATE MATERIALIZED VIEW IF NOT EXISTS users_by_email AS" +
                         $" SELECT * FROM {_options.KeyspaceName}.{CassandraSessionHelper.UsersTableName}" +
                         " WHERE NormalizedEmail IS NOT NULL AND Id IS NOT NULL" +
                         " PRIMARY KEY (NormalizedEmail, Id)");

        _session.Execute("CREATE MATERIALIZED VIEW IF NOT EXISTS users_by_username AS" +
                         $" SELECT * FROM {_options.KeyspaceName}.{CassandraSessionHelper.UsersTableName}" +
                         " WHERE NormalizedUserName IS NOT NULL AND Id IS NOT NULL" +
                         " PRIMARY KEY (NormalizedUserName, Id)");

        _session.Execute("CREATE MATERIALIZED VIEW IF NOT EXISTS roles_by_name AS" +
                         $" SELECT * FROM {_options.KeyspaceName}.{CassandraSessionHelper.RolesTableName}" +
                         " WHERE NormalizedName IS NOT NULL AND Id IS NOT NULL" +
                         " PRIMARY KEY (NormalizedName, Id)");

        _session.Execute("CREATE MATERIALIZED VIEW IF NOT EXISTS userclaims_by_type_and_value AS" +
                         $" SELECT * FROM {_options.KeyspaceName}.userclaims" +
                         " WHERE Type IS NOT NULL AND Value IS NOT NULL AND UserId IS NOT NULL" +
                         " PRIMARY KEY ((Type, Value), UserId)");
    }
}