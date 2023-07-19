using Cassandra;
using Cassandra.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System;
using System.Net.Sockets;

namespace AspNetCore.Identity.Cassandra.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCassandra(this IServiceCollection services, IConfiguration configuration, Action<ISession>? sessionCallback = null)
    {
        services.Configure<CassandraOptions>(configuration.GetSection("Cassandra"));
        services.AddSingleton<CqlQueryOptions>(x => 
            x.GetRequiredService<IOptions<CassandraOptions>>().Value.AsCqlQueryOptions());

        return services
            .AddTransient<IMapper>(serviceProvider =>
            {
                var session = serviceProvider.GetRequiredService<ISession>();
                return new Mapper(session);
            })
            .AddSingleton<ISession>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<CassandraOptions>>().Value;
                var logger = serviceProvider.GetRequiredService<ILogger<CassandraOptions>>();

                var queryOptions = new QueryOptions();
                if (options.Query is not null)
                {
                    queryOptions.SetConsistencyLevel(options.Query.ConsistencyLevel);
                }

                var cluster = Cluster.Builder()
                    .AddContactPoints(options.ContactPoints)
                    .WithPort(options.Port)
                    .WithCredentials(
                        options.Credentials.UserName,
                        options.Credentials.Password)
                    .WithQueryOptions(queryOptions)
                    .Build();

                ISession? session = null;
                Policy.Handle<SocketException>()
                    .Or<NoHostAvailableException>()
                    .WaitAndRetry(
                        options.RetryCount,
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        (exception, retryCount, context) => logger.LogWarning("Retry {RetryCount} due to: {ExceptionMessage}", retryCount, exception.Message))
                    .Execute(() => session = cluster.Connect());

                if (session is null)
                    throw new ApplicationException("FATAL ERROR: Cassandra session could not be created");

                sessionCallback?.Invoke(session);

                logger.LogInformation("Cassandra session has been created");
                return session;
            })
            .AddSingleton<DbInitializer>();
    }
}