using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Booklify.Infrastructure.Services.BackgroundJobs;

namespace Booklify.Infrastructure.Services;

/// <summary>
/// Hosted service to initialize Hangfire recurring jobs after Hangfire server starts
/// </summary>
public class HangfireInitializationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HangfireInitializationService> _logger;
    private readonly IConfiguration _configuration;

    public HangfireInitializationService(
        IServiceProvider serviceProvider,
        ILogger<HangfireInitializationService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Give Hangfire server a moment to fully initialize
            await Task.Delay(2000, cancellationToken);

            using var scope = _serviceProvider.CreateScope();
            var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

            // Get job schedules from configuration
            var cleanupCron = _configuration.GetValue<string>(
                "Hangfire:Jobs:Cleanup:Cron", 
                "0 3 * * 0"); // Default: Every Sunday at 3 AM

            var cleanupRetentionDays = _configuration.GetValue<int>(
                "Hangfire:Jobs:Cleanup:RetentionDays",
                30); // Default: 30 days

            // Schedule recurring cleanup job for soft-deleted records
            recurringJobManager.AddOrUpdate<FileDeleteJob>(
                "file-cleanup",
                job => job.ExecuteCleanupAsync(cleanupRetentionDays),
                cleanupCron);

            _logger.LogInformation("Hangfire recurring jobs initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Hangfire recurring jobs");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Hangfire initialization service stopping");
        return Task.CompletedTask;
    }
} 