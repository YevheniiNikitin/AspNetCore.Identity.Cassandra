using System;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Mapping;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AspNetCore.Identity.Cassandra.Extensions;

public static class MapperExtensions
{
    public static Task<IdentityResult> TryInsertAsync<TPoco>(this IMapper mapper,
        TPoco poco,
        CqlQueryOptions queryOptions,
        CassandraErrorDescriber errorDescriber,
        ILogger logger)
    {
        return TryExecuteAsync(() => mapper.InsertAsync(poco, queryOptions: queryOptions), errorDescriber, logger);
    }

    public static Task<IdentityResult> TryUpdateAsync<TPoco>(this IMapper mapper,
        TPoco poco,
        CqlQueryOptions queryOptions,
        CassandraErrorDescriber errorDescriber,
        ILogger logger)
    {
        return TryExecuteAsync(() => mapper.UpdateAsync(poco, queryOptions: queryOptions), errorDescriber, logger);
    }

    public static Task<IdentityResult> TryDeleteAsync<TPoco>(this IMapper mapper,
        TPoco poco,
        CqlQueryOptions queryOptions,
        CassandraErrorDescriber errorDescriber,
        ILogger logger)
    {
            
        return TryExecuteAsync(() => mapper.DeleteAsync(poco, queryOptions: queryOptions), errorDescriber, logger);
    }

    public static Task<IdentityResult> TryExecuteBatchAsync(this IMapper mapper,
        CassandraErrorDescriber errorDescriber,
        ILogger logger,
        CassandraQueryOptions? options,
        params Action<ICqlBatch>[] actions)
    {
        var batch = mapper
            .CreateBatch()
            .WithOptions(options);

        foreach (var action in actions)
            action(batch);

        return TryExecuteAsync(() => mapper.ExecuteAsync(batch), errorDescriber, logger);
    }

    private static async Task<IdentityResult> TryExecuteAsync(
        Func<Task> action,
        CassandraErrorDescriber errorDescriber,
        ILogger logger)
    {
        try
        {
            await action();
            return IdentityResult.Success;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error while executing query.");

            return exception switch
            {
                NoHostAvailableException => IdentityResult.Failed(errorDescriber.NoHostAvailable()),
                UnavailableException => IdentityResult.Failed(errorDescriber.Unavailable()),
                ReadTimeoutException => IdentityResult.Failed(errorDescriber.ReadTimeout()),
                WriteTimeoutException => IdentityResult.Failed(errorDescriber.WriteTimeout()),
                QueryValidationException => IdentityResult.Failed(errorDescriber.QueryValidation()),
                _ => IdentityResult.Failed(errorDescriber.DefaultError(exception.Message))
            };
        }
    }
}