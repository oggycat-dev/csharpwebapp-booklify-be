using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.Interfaces;
using Booklify.Domain.Enums;

namespace Booklify.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Background job for file deletion operations
/// </summary>
public class FileDeleteJob
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<FileDeleteJob> _logger;

    public FileDeleteJob(IServiceScopeFactory serviceScopeFactory, ILogger<FileDeleteJob> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Execute file deletion in background
    /// </summary>
    [Queue("file-operations")]
    public async Task ExecuteAsync(string filePath, string userId, Guid? fileInfoId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

        try
        {
            _logger.LogInformation("Starting background file deletion for file: {FilePath}", filePath);

            // Delete file from storage first (outside transaction)
            var success = await storageService.DeleteFileAsync(filePath);

            if (!success)
            {
                throw new InvalidOperationException($"Storage service failed to delete file: {filePath}");
            }

            // Begin transaction after successful file deletion
            if (fileInfoId.HasValue)
            {
                await unitOfWork.BeginTransactionAsync();

                var fileInfo = await unitOfWork.FileInfoRepository.GetByIdAsync(fileInfoId.Value);
                if (fileInfo != null)
                {
                    fileInfo.JobStatus = FileJobStatus.Completed;
                    fileInfo.JobCompletedAt = DateTime.UtcNow;
                    fileInfo.JobErrorMessage = null;
                    
                    // Soft delete the file info record
                    await unitOfWork.FileInfoRepository.SoftDeleteAsync(fileInfo, userId);
                }

                // Commit transaction
                await unitOfWork.CommitTransactionAsync();
            }

            var fileType = fileInfoId.HasValue ? "file" : "cover image";
            _logger.LogInformation("{FileType} deletion completed successfully: {FilePath}", fileType, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FilePath}: {Message}", filePath, ex.Message);

            // Rollback transaction if it was started
            try
            {
                await unitOfWork.RollbackTransactionAsync();
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Failed to rollback transaction");
            }

            throw;
        }
    }

    /// <summary>
    /// Execute batch file deletion in background
    /// </summary>
    [Queue("file-operations")]
    public async Task ExecuteBatchAsync(List<string> filePaths, string userId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

        _logger.LogInformation("Starting batch file deletion for {Count} files", filePaths.Count);

        var deletedFiles = new List<string>();
        var failedFiles = new List<string>();
        var fileInfosToDelete = new List<Domain.Entities.FileInfo>();

        // First find all FileInfo records in one query
        var fileInfos = await unitOfWork.FileInfoRepository
            .FindByCondition(f => filePaths.Contains(f.FilePath))
            .ToListAsync();

        var fileInfoMap = fileInfos.ToDictionary(f => f.FilePath, f => f);

        foreach (var filePath in filePaths)
        {
            try
            {
                var success = await storageService.DeleteFileAsync(filePath);
                
                if (success)
                {
                    deletedFiles.Add(filePath);
                    
                    // Add to batch delete if FileInfo exists
                    if (fileInfoMap.TryGetValue(filePath, out var fileInfo))
                    {
                        fileInfosToDelete.Add(fileInfo);
                    }

                    _logger.LogDebug("Successfully deleted file: {FilePath}", filePath);
                }
                else
                {
                    failedFiles.Add(filePath);
                    _logger.LogWarning("Storage service failed to delete file: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file {FilePath}: {Message}", filePath, ex.Message);
                failedFiles.Add(filePath);
            }
        }

        // Batch soft delete all successful FileInfo records
        if (fileInfosToDelete.Any())
        {
            try
            {
                await unitOfWork.BeginTransactionAsync();
                await unitOfWork.FileInfoRepository.SoftDeleteRangeAsync(fileInfosToDelete, userId);
                await unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                try
                {
                    await unitOfWork.RollbackTransactionAsync();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Failed to rollback transaction during batch delete");
                }
                
                _logger.LogError(ex, "Failed to batch soft delete {Count} FileInfo records", fileInfosToDelete.Count);
                throw;
            }
        }

        _logger.LogInformation("Batch deletion completed. Successful: {SuccessCount}, Failed: {FailedCount}", 
            deletedFiles.Count, failedFiles.Count);

        if (failedFiles.Any())
        {
            throw new InvalidOperationException($"Failed to delete {failedFiles.Count} files: {string.Join(", ", failedFiles)}");
        }
    }

    /// <summary>
    /// Permanently delete soft-deleted records that have no physical files after retention period
    /// </summary>
    /// <param name="retentionDays">Number of days to keep soft-deleted records before permanent deletion</param>
    [Queue("file-operations")]
    public async Task ExecuteCleanupAsync(int retentionDays = 30)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

        try
        {
            _logger.LogInformation("Starting cleanup of soft-deleted records older than {Days} days", retentionDays);

            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            const int batchSize = 100;
            int processedCount = 0;
            int totalDeleted = 0;
            bool hasMore = true;

            while (hasMore)
            {
                // Get batch of soft-deleted records older than retention period
                var recordsToCheck = await unitOfWork.FileInfoRepository
                    .FindByConditionIncludeDeleted(f => 
                        f.IsDeleted == true && 
                        f.DeletedAt.HasValue && 
                        f.DeletedAt.Value < cutoffDate)
                    .Skip(processedCount)
                    .Take(batchSize)
                    .ToListAsync();

                if (!recordsToCheck.Any())
                {
                    hasMore = false;
                    break;
                }

                var recordsToDelete = new List<Domain.Entities.FileInfo>();

                // Check which records have no physical files
                foreach (var record in recordsToCheck)
                {
                    try
                    {
                        var fileExists = await storageService.FileExistsAsync(record.FilePath);
                        
                        // If file doesn't exist physically, add to delete list
                        if (!fileExists)
                        {
                            recordsToDelete.Add(record);
                        }
                    }
                    catch
                    {
                        // If we can't access the file, assume it doesn't exist
                        recordsToDelete.Add(record);
                    }
                }

                // Permanently delete records that have no physical files
                if (recordsToDelete.Any())
                {
                    try
                    {
                        await unitOfWork.BeginTransactionAsync();
                        await unitOfWork.FileInfoRepository.DeleteRangeAsync(recordsToDelete);
                        await unitOfWork.CommitTransactionAsync();
                        totalDeleted += recordsToDelete.Count;
                        
                        _logger.LogInformation(
                            "Permanently deleted {Count} records with no physical files", 
                            recordsToDelete.Count);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            await unitOfWork.RollbackTransactionAsync();
                        }
                        catch (Exception rollbackEx)
                        {
                            _logger.LogError(rollbackEx, "Failed to rollback transaction during cleanup");
                        }
                        
                        _logger.LogError(ex, "Failed to permanently delete {Count} records", recordsToDelete.Count);
                        throw;
                    }
                }

                processedCount += recordsToCheck.Count;
                _logger.LogInformation(
                    "Processed {ProcessedCount} records, permanently deleted {DeletedCount} records in this batch",
                    recordsToCheck.Count, recordsToDelete.Count);
            }

            _logger.LogInformation(
                "Cleanup completed. Processed {ProcessedCount} records, permanently deleted {DeletedCount} records",
                processedCount, totalDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup: {Message}", ex.Message);
            throw;
        }
    }
} 