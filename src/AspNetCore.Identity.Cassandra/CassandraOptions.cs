using System.Collections.Generic;
using Cassandra;

namespace AspNetCore.Identity.Cassandra;

public class CassandraOptions
{
    public List<string> ContactPoints { get; set; } = null!;
    public int Port { get; set; } = 9042;
    public int RetryCount { get; set; } = 3;
    public CassandraCredentials Credentials { get; set; } = null!;
    public string KeyspaceName { get; set; } = null!;
    public Dictionary<string, string>? Replication { get; set; } = null;
    public bool DurableWrites { get; set; } = true;
    public CassandraQueryOptions? Query { get; set; }
}

public class CassandraCredentials
{
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class CassandraQueryOptions
{
    public ConsistencyLevel ConsistencyLevel { get; set; }
    public bool? TracingEnabled { get; set; }
    public int? PageSize { get; set; }
}