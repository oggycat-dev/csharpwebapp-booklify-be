using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;
using Booklify.Infrastructure.Services.BackgroundJobs;
using Booklify.Infrastructure.Models;
using Booklify.Infrastructure.Utils;

namespace Booklify.Infrastructure.Services;

/// <summary>
/// Implementation of file background service using Hangfire
/// </summary>
public class FileBackgroundService : IFileBackgroundService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<FileBackgroundService> _logger;

    public FileBackgroundService(
        IBackgroundJobClient backgroundJobClient,
        ILogger<FileBackgroundService> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    /// <summary>
    /// Queue a file upload job in the background
    /// </summary>
    public string QueueFileUpload(IFormFile file, string subDirectory = "", string userId = "", FileUploadType uploadType = FileUploadType.None, Guid? entityId = null)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is null or empty", nameof(file));

        try
        {
            // Convert IFormFile to byte array for serialization
            using var memoryStream = new MemoryStream();
            file.CopyTo(memoryStream);
            var fileBytes = memoryStream.ToArray();

            // Create unique, sanitized file name
            var fileName = FileNameSanitizer.CreateUniqueFileName(file.FileName);

            // Queue the job
            var jobId = _backgroundJobClient.Enqueue<FileUploadJob>(
                job => job.ExecuteAsync(fileBytes, fileName, file.ContentType, subDirectory, userId, uploadType, entityId));

            _logger.LogInformation("Queued file upload job {JobId} for file: {FileName} with type {UploadType}",
                jobId, file.FileName, uploadType);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue file upload job for file: {FileName}", file.FileName);
            throw;
        }
    }

    /// <summary>
    /// Queue a file deletion job in the background
    /// </summary>
    public string QueueFileDelete(string filePath, string userId = "", Guid? fileInfoId = null)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path is null or empty", nameof(filePath));

        try
        {
            var jobId = _backgroundJobClient.Enqueue<FileDeleteJob>(
                job => job.ExecuteAsync(filePath, userId, fileInfoId));

            _logger.LogInformation("Queued file deletion job {JobId} for file: {FilePath}", jobId, filePath);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue file deletion job for file: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Queue a batch file upload job in the background
    /// </summary>
    public string QueueBatchFileUpload(IEnumerable<IFormFile> files, string subDirectory = "", string userId = "")
    {
        if (files == null || !files.Any())
            throw new ArgumentException("Files collection is null or empty", nameof(files));

        try
        {
            var fileDataList = new List<FileUploadData>();

            foreach (var file in files)
            {
                if (file != null && file.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    file.CopyTo(memoryStream);
                    var fileBytes = memoryStream.ToArray();

                    var fileName = FileNameSanitizer.CreateUniqueFileName(file.FileName);

                    fileDataList.Add(new FileUploadData
                    {
                        FileBytes = fileBytes,
                        FileName = fileName,
                        ContentType = file.ContentType
                    });
                }
            }

            if (!fileDataList.Any())
                throw new ArgumentException("No valid files found in the collection");

            var jobId = _backgroundJobClient.Enqueue<FileUploadJob>(
                job => job.ExecuteBatchAsync(fileDataList, subDirectory, userId));

            _logger.LogInformation("Queued batch file upload job {JobId} for {Count} files", jobId, fileDataList.Count);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue batch file upload job");
            throw;
        }
    }

    /// <summary>
    /// Queue a batch file deletion job in the background
    /// </summary>
    public string QueueBatchFileDelete(IEnumerable<string> filePaths, string userId = "")
    {
        if (filePaths == null || !filePaths.Any())
            throw new ArgumentException("File paths collection is null or empty", nameof(filePaths));

        try
        {
            var filePathList = filePaths.Where(path => !string.IsNullOrEmpty(path)).ToList();

            if (!filePathList.Any())
                throw new ArgumentException("No valid file paths found in the collection");

            var jobId = _backgroundJobClient.Enqueue<FileDeleteJob>(
                job => job.ExecuteBatchAsync(filePathList, userId));

            _logger.LogInformation("Queued batch file deletion job {JobId} for {Count} files", jobId, filePathList.Count);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue batch file deletion job");
            throw;
        }
    }

    /// <summary>
    /// Get job status for a specific job ID
    /// </summary>
    public Task<FileJobStatusInfo> GetJobStatusAsync(string jobId)
    {
        if (string.IsNullOrEmpty(jobId))
            throw new ArgumentException("Job ID is null or empty", nameof(jobId));

        try
        {
            var jobDetails = JobStorage.Current.GetConnection().GetJobData(jobId);

            if (jobDetails == null)
            {
                return Task.FromResult(new FileJobStatusInfo
                {
                    JobId = jobId,
                    Status = FileJobStatus.None,
                    ErrorMessage = "Job not found"
                });
            }

            var status = MapHangfireStateToFileJobStatus(jobDetails.State);
            var startedAt = jobDetails.CreatedAt;
            var completedAt = GetJobCompletedTime(jobId);

            return Task.FromResult(new FileJobStatusInfo
            {
                JobId = jobId,
                Status = status,
                StartedAt = startedAt,
                CompletedAt = completedAt,
                StateDisplayName = jobDetails.State,
                ErrorMessage = GetJobErrorMessage(jobId)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get job status for job: {JobId}", jobId);

            return Task.FromResult(new FileJobStatusInfo
            {
                JobId = jobId,
                Status = FileJobStatus.Failed,
                ErrorMessage = ex.Message
            });
        }
    }

    /// <summary>
    /// Cancel a running or pending job
    /// </summary>
    public bool CancelJob(string jobId)
    {
        if (string.IsNullOrEmpty(jobId))
            throw new ArgumentException("Job ID is null or empty", nameof(jobId));

        try
        {
            var result = _backgroundJobClient.Delete(jobId);

            if (result)
            {
                _logger.LogInformation("Successfully cancelled job: {JobId}", jobId);
            }
            else
            {
                _logger.LogWarning("Failed to cancel job: {JobId}", jobId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling job: {JobId}", jobId);
            return false;
        }
    }

    /// <summary>
    /// Queue a cleanup job for soft-deleted files
    /// </summary>
    public string QueueCleanup(int retentionDays = 30)
    {
        try
        {
            var jobId = _backgroundJobClient.Enqueue<FileDeleteJob>(
                job => job.ExecuteCleanupAsync(retentionDays));

            _logger.LogInformation("Queued file cleanup job {JobId} with retention days: {RetentionDays}",
                jobId, retentionDays);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue file cleanup job");
            throw;
        }
    }

    /// <summary>
    /// Schedule recurring cleanup of soft-deleted files
    /// </summary>
    public void ScheduleRecurringCleanup(string cronExpression = "0 3 * * 0", int retentionDays = 30) // Every Sunday at 3 AM
    {
        try
        {
            RecurringJob.AddOrUpdate<FileDeleteJob>(
                "file-cleanup",
                job => job.ExecuteCleanupAsync(retentionDays),
                cronExpression);

            _logger.LogInformation(
                "Scheduled recurring file cleanup with cron: {CronExpression}, retention days: {RetentionDays}", 
                cronExpression, retentionDays);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule recurring file cleanup");
            throw;
        }
    }

    /// <summary>
    /// Queue an EPUB processing job
    /// </summary>
    public string QueueEpubProcessing(Guid bookId, string userId = "")
    {
        try
        {
            var jobId = _backgroundJobClient.Enqueue<EpubProcessingJob>(
                job => job.ExecuteAsync(bookId, userId));

            _logger.LogInformation("Queued EPUB processing job {JobId} for book {BookId}", 
                jobId, bookId);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue EPUB processing job for book {BookId}", bookId);
            throw;
        }
    }

    /// <summary>
    /// Queue a chapter deletion job by book ID
    /// </summary>
    public string QueueChapterDeletionByBookId(Guid bookId, string userId = "")
    {
        try
        {
            var jobId = _backgroundJobClient.Enqueue<ChapterDeletionJob>(
                job => job.DeleteChaptersByBookIdAsync(bookId, userId));

            _logger.LogInformation("Queued chapter deletion job {JobId} for book {BookId}", 
                jobId, bookId);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue chapter deletion job for book {BookId}", bookId);
            throw;
        }
    }

    /// <summary>
    /// Queue a chapter deletion job by chapter IDs
    /// </summary>
    public string QueueChapterDeletionByIds(List<Guid> chapterIds, string userId = "")
    {
        if (chapterIds == null || !chapterIds.Any())
            throw new ArgumentException("Chapter IDs collection is null or empty", nameof(chapterIds));

        try
        {
            var jobId = _backgroundJobClient.Enqueue<ChapterDeletionJob>(
                job => job.DeleteChaptersByIdsAsync(chapterIds, userId));

            _logger.LogInformation("Queued chapter deletion job {JobId} for {ChapterCount} chapters", 
                jobId, chapterIds.Count);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue chapter deletion job for {ChapterCount} chapters", chapterIds.Count);
            throw;
        }
    }

    #region Private Helper Methods

    private static FileJobStatus MapHangfireStateToFileJobStatus(string hangfireState)
    {
        return hangfireState?.ToLowerInvariant() switch
        {
            "enqueued" => FileJobStatus.Pending,
            "processing" => FileJobStatus.Processing,
            "succeeded" => FileJobStatus.Completed,
            "failed" => FileJobStatus.Failed,
            "deleted" => FileJobStatus.Cancelled,
            _ => FileJobStatus.None
        };
    }

    private static DateTime? GetJobCompletedTime(string jobId)
    {
        try
        {
            var connection = JobStorage.Current.GetConnection();
            var jobData = connection.GetJobData(jobId);

            if (jobData != null && jobData.State != null)
            {
                // For completed jobs, we can use the created time plus some estimation
                // This is a simplified approach since GetJobHistory is not available
                if (jobData.State.Equals("Succeeded", StringComparison.OrdinalIgnoreCase) ||
                    jobData.State.Equals("Failed", StringComparison.OrdinalIgnoreCase))
                {
                    return DateTime.UtcNow; // Fallback to current time
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetJobErrorMessage(string jobId)
    {
        try
        {
            var connection = JobStorage.Current.GetConnection();
            var jobData = connection.GetJobData(jobId);

            if (jobData != null && jobData.State?.Equals("Failed", StringComparison.OrdinalIgnoreCase) == true)
            {
                // Try to get state data which may contain exception information
                var stateData = connection.GetStateData(jobId);
                return stateData?.Data?.ContainsKey("ExceptionMessage") == true
                    ? stateData.Data["ExceptionMessage"]
                    : stateData?.Reason;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    #endregion
} 