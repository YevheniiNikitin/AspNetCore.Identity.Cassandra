using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AspNetCore.Identity.Cassandra;

public class CassandraIdentityInfrastructureInitializer<TUser, TRole> : BackgroundService
{
    public CassandraIdentityInfrastructureInitializer(
        ILogger<CassandraIdentityInfrastructureInitializer<TUser, TRole>> logger,
        DbInitializer initializer)
    {
        _logger = logger;
        _initializer = initializer;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _initializer.Initialize<TUser, TRole>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Cassandra Identity.");
            throw;
        }

        return Task.CompletedTask;
    }


    private readonly ILogger<CassandraIdentityInfrastructureInitializer<TUser, TRole>> _logger;
    private readonly DbInitializer _initializer;

}