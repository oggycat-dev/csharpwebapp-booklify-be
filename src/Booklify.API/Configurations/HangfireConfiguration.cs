using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.Interfaces;
using Booklify.Infrastructure.Filters;
using Booklify.Infrastructure.Services;
using Booklify.Infrastructure.Services.BackgroundJobs;

namespace Booklify.API.Configurations;

/// <summary>
/// Extension methods for Hangfire configuration
/// </summary>
public static class HangfireConfiguration
{
    /// <summary>
    /// Add Hangfire services to the dependency container
    /// </summary>
    public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure logging to suppress Hangfire logs
        services.AddLogging(builder =>
        {
            builder.AddFilter("Hangfire", LogLevel.None);
            builder.AddFilter("Hangfire.BackgroundJobServer", LogLevel.None);
            builder.AddFilter("Hangfire.SqlServer", LogLevel.None);
            builder.AddFilter("Hangfire.Processing", LogLevel.None);
        });

        // Get connection string
        var connectionString = configuration["Hangfire:ConnectionString"];
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string is required for Hangfire");
        }

        // Get Hangfire settings from configuration
        var commandBatchMaxTimeout = configuration.GetValue("Hangfire:CommandBatchMaxTimeout", 300);
        var slidingInvisibilityTimeout = configuration.GetValue("Hangfire:SlidingInvisibilityTimeout", 300);
        var queuePollInterval = configuration.GetValue("Hangfire:QueuePollInterval", 0);
        var useRecommendedIsolationLevel = configuration.GetValue("Hangfire:UseRecommendedIsolationLevel", true);
        var disableGlobalLocks = configuration.GetValue("Hangfire:DisableGlobalLocks", true);

        // Get retry settings
        var retryAttempts = configuration.GetValue("Hangfire:Retry:Attempts", 3);
        var retryDelayFirst = configuration.GetValue("Hangfire:Retry:DelayInSeconds:First", 60);
        var retryDelaySecond = configuration.GetValue("Hangfire:Retry:DelayInSeconds:Second", 300);
        var retryDelayThird = configuration.GetValue("Hangfire:Retry:DelayInSeconds:Third", 600);

        // Get job limit settings
        var maxConcurrentJobs = configuration.GetValue("Hangfire:Limits:MaxConcurrentJobsPerQueue", 5);
        var queueTimeoutSeconds = configuration.GetValue("Hangfire:Limits:QueueTimeout", 600);

        // Add Hangfire services
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                PrepareSchemaIfNecessary = false,
                CommandBatchMaxTimeout = TimeSpan.FromSeconds(commandBatchMaxTimeout),
                SlidingInvisibilityTimeout = TimeSpan.FromSeconds(slidingInvisibilityTimeout),
                QueuePollInterval = TimeSpan.FromSeconds(queuePollInterval),
                UseRecommendedIsolationLevel = useRecommendedIsolationLevel,
                DisableGlobalLocks = disableGlobalLocks
            })
            // Add retry policy
            .UseFilter(new AutomaticRetryAttribute 
            { 
                Attempts = retryAttempts,
                DelaysInSeconds = new[] { retryDelayFirst, retryDelaySecond, retryDelayThird }
                    .Take(retryAttempts)  // Chỉ lấy số delay tương ứng với số lần retry
                    .ToArray()
            })
            // Add job filter for limiting concurrent executions
            .UseFilter(new JobConcurrencyFilterAttribute
            {
                MaxConcurrentExecutions = maxConcurrentJobs,
                QueueTimeout = TimeSpan.FromSeconds(queueTimeoutSeconds)
            }));

        // Get server settings from configuration
        var heartbeatInterval = configuration.GetValue("Hangfire:HeartbeatInterval", 30);
        var workerCount = configuration.GetValue("Hangfire:WorkerCount", 0);
        var queues = configuration.GetValue("Hangfire:Queues", "default,file-operations,epub-processing")
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? new[] { "default" };

        // Add Hangfire server
        services.AddHangfireServer(options =>
        {
            options.HeartbeatInterval = TimeSpan.FromSeconds(heartbeatInterval);
            options.Queues = queues;
            options.WorkerCount = workerCount > 0 ? workerCount : Environment.ProcessorCount * 2;
        });

        // Register background job services
        services.AddScoped<FileUploadJob>();
        services.AddScoped<FileDeleteJob>();
        services.AddScoped<EpubProcessingJob>();
        services.AddScoped<ChapterDeletionJob>();
        services.AddScoped<IFileBackgroundService, FileBackgroundService>();

        return services;
    }

    /// <summary>
    /// Configure Hangfire dashboard and initialize recurring jobs
    /// </summary>
    public static void UseHangfireConfiguration(this IServiceProvider serviceProvider)
    {
        // Get the recurring job manager and configuration using the service provider
        using var scope = serviceProvider.CreateScope();
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Get job schedules from configuration
        var cleanupCron = configuration.GetValue<string>(
            "Hangfire:Jobs:Cleanup:Cron", 
            "0 3 * * 0"); // Default: Every Sunday at 3 AM

        var cleanupRetentionDays = configuration.GetValue<int>(
            "Hangfire:Jobs:Cleanup:RetentionDays",
            30); // Default: 30 days
        
        // Schedule recurring cleanup job for soft-deleted records
        recurringJobManager.AddOrUpdate<FileDeleteJob>(
            "file-cleanup",
            job => job.ExecuteCleanupAsync(cleanupRetentionDays),
            cleanupCron);
    }
} 