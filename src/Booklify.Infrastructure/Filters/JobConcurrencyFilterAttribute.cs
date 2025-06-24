using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;

namespace Booklify.Infrastructure.Filters;

/// <summary>
/// Hangfire filter to limit concurrent job executions per queue
/// </summary>
public class JobConcurrencyFilterAttribute : JobFilterAttribute, IElectStateFilter
{
    public int MaxConcurrentExecutions { get; set; } = 1;
    public TimeSpan QueueTimeout { get; set; } = TimeSpan.FromMinutes(10);

    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is ProcessingState)
        {
            var connection = context.Connection;
            var queueName = EnqueuedState.DefaultQueue;

            // Get the queue name from the job if available
            if (context.BackgroundJob.Job.Args.Count > 0 && context.BackgroundJob.Job.Args[0] is string queue)
            {
                queueName = queue;
            }

            // Use a simpler approach - get all processing jobs and count them
            var processingJobsInQueue = 0;
            try
            {
                var processingJobs = JobStorage.Current.GetMonitoringApi().ProcessingJobs(0, int.MaxValue);
                processingJobsInQueue = processingJobs.Count();
            }
            catch
            {
                // If monitoring fails, allow the job to proceed
                processingJobsInQueue = 0;
            }

            if (processingJobsInQueue >= MaxConcurrentExecutions)
            {
                // Delay the job
                context.CandidateState = new ScheduledState(DateTime.UtcNow.Add(QueueTimeout))
                {
                    Reason = $"Concurrency limit ({MaxConcurrentExecutions}) reached for queue '{queueName}'"
                };
            }
        }
    }
} 