using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;
using Booklify.Infrastructure.Utils;

namespace Booklify.Infrastructure.Services;

/// <summary>
/// Implementation of the file service using the appropriate storage provider
/// </summary>
public class FileService : IFileService
{
    private readonly IStorageService _storageService;
    private readonly IFileBackgroundService _fileBackgroundService;
    private readonly ILogger<FileService> _logger;

    public FileService(
        IStorageService storageService,
        IFileBackgroundService fileBackgroundService,
        ILogger<FileService> logger)
    {
        _storageService = storageService;
        _fileBackgroundService = fileBackgroundService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a file to storage (synchronous)
    /// </summary>
    public async Task<Result<FileUploadResult>> UploadFileAsync(IFormFile file, string subDirectory = "", string userId = "")
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return Result<FileUploadResult>.Failure("File is empty");
            }

            // Create unique, sanitized file name to prevent overwriting and ensure URL safety
            var fileName = FileNameSanitizer.CreateUniqueFileName(file.FileName);
            
            // Use storage service to upload the file
            using var fileStream = file.OpenReadStream();
            var storagePath = await _storageService.UploadFileAsync(fileStream, fileName, file.ContentType, subDirectory);
            
            // Get file extension
            var extension = Path.GetExtension(file.FileName).TrimStart('.');
            
            // Create result object
            var result = new FileUploadResult
            {
                OriginalFileName = file.FileName,
                FileName = fileName,
                SizeKb = file.Length / 1024, // Convert to KB
                FilePath = storagePath,
                MimeType = file.ContentType,
                Extension = extension
            };
            
            return Result<FileUploadResult>.Success(result, "File uploaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {Message}", ex.Message);
            return Result<FileUploadResult>.Failure($"Error uploading file: {ex.Message}");
        }
    }

    /// <summary>
    /// Queue a file upload in background
    /// </summary>
    public Result<string> QueueFileUploadAsync(
        IFormFile file, 
        string subDirectory = "", 
        string userId = "", 
        FileUploadType uploadType = FileUploadType.None,
        Guid? entityId = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return Result<string>.Failure("File is empty");
            }

            var jobId = _fileBackgroundService.QueueFileUpload(
                file, 
                subDirectory, 
                userId,
                uploadType,
                entityId);
            
            _logger.LogInformation(
                "File upload queued with job ID: {JobId} for type {UploadType}", 
                jobId, 
                uploadType);

            return Result<string>.Success(jobId, "File upload queued successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queueing file upload: {Message}", ex.Message);
            return Result<string>.Failure($"Error queueing file upload: {ex.Message}");
        }
    }

    /// <summary>
    /// Queue batch file upload in background
    /// </summary>
    public Result<string> QueueBatchFileUploadAsync(IEnumerable<IFormFile> files, string subDirectory = "", string userId = "")
    {
        try
        {
            if (files == null || !files.Any())
            {
                return Result<string>.Failure("No files provided");
            }

            var validFiles = files.Where(f => f != null && f.Length > 0).ToList();
            if (!validFiles.Any())
            {
                return Result<string>.Failure("No valid files found");
            }

            var jobId = _fileBackgroundService.QueueBatchFileUpload(validFiles, subDirectory, userId);
            
            _logger.LogInformation("Batch file upload queued with job ID: {JobId} for {Count} files", jobId, validFiles.Count);
            return Result<string>.Success(jobId, $"Batch upload of {validFiles.Count} files queued successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queueing batch file upload: {Message}", ex.Message);
            return Result<string>.Failure($"Error queueing batch file upload: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a file from storage (synchronous)
    /// </summary>
    public async Task<Result> DeleteFileAsync(string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return Result.Failure("File path is empty");
            }
            
            // Use storage service to delete the file
            var success = await _storageService.DeleteFileAsync(filePath);
            
            if (success)
            {
                return Result.Success("File deleted successfully");
            }
            else
            {
                return Result.Failure("File could not be deleted");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {Message}", ex.Message);
            return Result.Failure($"Error deleting file: {ex.Message}");
        }
    }

    /// <summary>
    /// Queue a file deletion in background
    /// </summary>
    public Result<string> QueueFileDeleteAsync(string filePath, string userId = "", Guid? fileInfoId = null)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return Result<string>.Failure("File path is empty");
            }

            var jobId = _fileBackgroundService.QueueFileDelete(filePath, userId, fileInfoId);
            
            _logger.LogInformation("File deletion queued with job ID: {JobId}", jobId);
            return Result<string>.Success(jobId, "File deletion queued successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queueing file deletion: {Message}", ex.Message);
            return Result<string>.Failure($"Error queueing file deletion: {ex.Message}");
        }
    }

    /// <summary>
    /// Queue batch file deletion in background
    /// </summary>
    public Result<string> QueueBatchFileDeleteAsync(IEnumerable<string> filePaths, string userId = "")
    {
        try
        {
            if (filePaths == null || !filePaths.Any())
            {
                return Result<string>.Failure("No file paths provided");
            }

            var validPaths = filePaths.Where(path => !string.IsNullOrEmpty(path)).ToList();
            if (!validPaths.Any())
            {
                return Result<string>.Failure("No valid file paths found");
            }

            var jobId = _fileBackgroundService.QueueBatchFileDelete(validPaths, userId);
            
            _logger.LogInformation("Batch file deletion queued with job ID: {JobId} for {Count} files", jobId, validPaths.Count);
            return Result<string>.Success(jobId, $"Batch deletion of {validPaths.Count} files queued successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queueing batch file deletion: {Message}", ex.Message);
            return Result<string>.Failure($"Error queueing batch file deletion: {ex.Message}");
        }
    }

    /// <summary>
    /// Get file job status
    /// </summary>
    public async Task<Result<FileJobStatusInfo>> GetFileJobStatusAsync(string jobId)
    {
        try
        {
            if (string.IsNullOrEmpty(jobId))
            {
                return Result<FileJobStatusInfo>.Failure("Job ID is empty");
            }

            var status = await _fileBackgroundService.GetJobStatusAsync(jobId);
            return Result<FileJobStatusInfo>.Success(status, "Job status retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job status: {Message}", ex.Message);
            return Result<FileJobStatusInfo>.Failure($"Error getting job status: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancel a file job
    /// </summary>
    public Result<bool> CancelFileJob(string jobId)
    {
        try
        {
            if (string.IsNullOrEmpty(jobId))
            {
                return Result<bool>.Failure("Job ID is empty");
            }

            var cancelled = _fileBackgroundService.CancelJob(jobId);
            
            if (cancelled)
            {
                return Result<bool>.Success(true, "Job cancelled successfully");
            }
            else
            {
                return Result<bool>.Failure("Failed to cancel job");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling job: {Message}", ex.Message);
            return Result<bool>.Failure($"Error cancelling job: {ex.Message}");
        }
    }

    /// <summary>
    /// Get file from storage by path
    /// </summary>
    public async Task<Result<(byte[] FileContent, string ContentType, string FileName)>> GetFileAsync(string filePath, string originalFileName)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return Result<(byte[] FileContent, string ContentType, string FileName)>.Failure("File path is empty");
            }

            // Get the file content from storage
            var fileStream = await _storageService.DownloadFileAsync(filePath);
            
            if (fileStream == null)
            {
                return Result<(byte[] FileContent, string ContentType, string FileName)>.Failure("File not found or empty");
            }

            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            var fileContent = memoryStream.ToArray();

            if (fileContent.Length == 0)
            {
                return Result<(byte[] FileContent, string ContentType, string FileName)>.Failure("File is empty");
            }

            // Get content type from file info
            var fileInfo = await _storageService.GetFileInfoAsync(filePath);
            var contentType = fileInfo?.ContentType ?? "application/octet-stream";

            // Use the original file name if available, otherwise use the file name from the path
            string fileName = !string.IsNullOrEmpty(originalFileName) 
                ? originalFileName 
                : Path.GetFileName(filePath);

            return Result<(byte[] FileContent, string ContentType, string FileName)>.Success(
                (fileContent, contentType, fileName), 
                "File retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file: {Message}", ex.Message);
            return Result<(byte[] FileContent, string ContentType, string FileName)>.Failure($"Error retrieving file: {ex.Message}");
        }
    }

    /// <summary>
    /// Get the public URL for a file
    /// </summary>
    public string GetFileUrl(string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogWarning("File path is empty when getting file URL");
                return string.Empty;
            }

            return _storageService.GetFileUrl(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file URL for path: {FilePath}", filePath);
            return filePath; // Fallback to file path
        }
    }
} 