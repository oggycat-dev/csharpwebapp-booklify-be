using Microsoft.AspNetCore.Http;
using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;

namespace Booklify.Application.Common.Interfaces;

/// <summary>
/// Interface for background file operations service
/// </summary>
public interface IFileBackgroundService
{
    /// <summary>
    /// Queue a file upload job in the background
    /// </summary>
    string QueueFileUpload(IFormFile file, string subDirectory = "", string userId = "", FileUploadType uploadType = FileUploadType.None, Guid? entityId = null);

    /// <summary>
    /// Queue a file deletion job in the background
    /// </summary>
    string QueueFileDelete(string filePath, string userId = "", Guid? fileInfoId = null);

    /// <summary>
    /// Queue a batch file upload job in the background
    /// </summary>
    string QueueBatchFileUpload(IEnumerable<IFormFile> files, string subDirectory = "", string userId = "");

    /// <summary>
    /// Queue a batch file deletion job in the background
    /// </summary>
    string QueueBatchFileDelete(IEnumerable<string> filePaths, string userId = "");

    /// <summary>
    /// Get job status for a specific job ID
    /// </summary>
    Task<FileJobStatusInfo> GetJobStatusAsync(string jobId);

    /// <summary>
    /// Cancel a running or pending job
    /// </summary>
    bool CancelJob(string jobId);

    /// <summary>
    /// Queue a cleanup job for soft-deleted files
    /// </summary>
    string QueueCleanup(int retentionDays = 30);

    /// <summary>
    /// Schedule recurring cleanup of soft-deleted files
    /// </summary>
    void ScheduleRecurringCleanup(string cronExpression = "0 3 * * 0", int retentionDays = 30);

    /// <summary>
    /// Queue an EPUB processing job with pre-downloaded content
    /// </summary>
    string QueueEpubProcessingWithContent(Guid bookId, string userId, byte[] fileContent, string fileExtension);

    /// <summary>
    /// Queue a chapter deletion job by book ID
    /// </summary>
    string QueueChapterDeletionByBookId(Guid bookId, string userId = "");

    /// <summary>
    /// Queue a chapter deletion job by chapter IDs
    /// </summary>
    string QueueChapterDeletionByIds(List<Guid> chapterIds, string userId = "");
} 