using Cassandra.Mapping;

namespace AspNetCore.Identity.Cassandra.Extensions;

public static class CqlBatchExtensions
{
    public static ICqlBatch WithOptions(this ICqlBatch batch, CassandraQueryOptions? queryOptions) =>
        batch.WithOptions(options =>
        {
            if (queryOptions is null)
                return;
            
            options.SetConsistencyLevel(queryOptions.ConsistencyLevel);

            if (queryOptions.PageSize.HasValue)
                options.SetPageSize(queryOptions.PageSize.Value);

            if (queryOptions.TracingEnabled.HasValue is false) 
                return;
            
            if (queryOptions.TracingEnabled.Value)
                options.EnableTracing();
            else
                options.DisableTracing();
        });
}