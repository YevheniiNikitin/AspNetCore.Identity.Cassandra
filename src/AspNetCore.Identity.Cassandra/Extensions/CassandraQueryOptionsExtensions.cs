using Cassandra.Mapping;

namespace AspNetCore.Identity.Cassandra.Extensions;

public static class CassandraQueryOptionsExtensions
{
    public static CqlQueryOptions AsCqlQueryOptions(this CassandraOptions options)
    {
        var cqlQueryOptions = CqlQueryOptions.New();
        
        if (options.Query is null)
            return cqlQueryOptions;

        cqlQueryOptions.SetConsistencyLevel(options.Query.ConsistencyLevel);

        if (options.Query.PageSize.HasValue)
            cqlQueryOptions.SetPageSize(options.Query.PageSize.Value);

        if (options.Query.TracingEnabled.HasValue is false)
            return cqlQueryOptions;

        if (options.Query.TracingEnabled.Value)
            cqlQueryOptions.EnableTracing();
        else
            cqlQueryOptions.DisableTracing();

        return cqlQueryOptions;
    }
}