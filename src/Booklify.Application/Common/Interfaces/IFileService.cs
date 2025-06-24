using Microsoft.AspNetCore.Http;
using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;

namespace Booklify.Application.Common.Interfaces;

/// <summary>
/// Interface for file operations service
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Upload a file to storage (synchronous)
    /// </summary>
    Task<Result<FileUploadResult>> UploadFileAsync(IFormFile file, string subDirectory = "", string userId = "");

    /// <summary>
    /// Queue a file upload in background
    /// </summary>
    Result<string> QueueFileUploadAsync(
        IFormFile file, 
        string subDirectory = "", 
        string userId = "", 
        FileUploadType uploadType = FileUploadType.None,
        Guid? entityId = null);

    /// <summary>
    /// Queue batch file upload in background
    /// </summary>
    Result<string> QueueBatchFileUploadAsync(IEnumerable<IFormFile> files, string subDirectory = "", string userId = "");

    /// <summary>
    /// Delete a file from storage (synchronous)
    /// </summary>
    Task<Result> DeleteFileAsync(string filePath);

    /// <summary>
    /// Queue a file deletion in background
    /// </summary>
    Result<string> QueueFileDeleteAsync(string filePath, string userId = "", Guid? fileInfoId = null);

    /// <summary>
    /// Queue batch file deletion in background
    /// </summary>
    Result<string> QueueBatchFileDeleteAsync(IEnumerable<string> filePaths, string userId = "");

    /// <summary>
    /// Get file job status
    /// </summary>
    Task<Result<FileJobStatusInfo>> GetFileJobStatusAsync(string jobId);

    /// <summary>
    /// Cancel a file job
    /// </summary>
    Result<bool> CancelFileJob(string jobId);

    /// <summary>
    /// Get file from storage by path
    /// </summary>
    Task<Result<(byte[] FileContent, string ContentType, string FileName)>> GetFileAsync(string filePath, string originalFileName);

    /// <summary>
    /// Get the public URL for a file
    /// </summary>
    string GetFileUrl(string filePath);
}

/// <summary>
/// File upload result model
/// </summary>
public class FileUploadResult
{
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long SizeKb { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
} 