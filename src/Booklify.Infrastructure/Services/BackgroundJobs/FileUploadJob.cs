using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Entities;
using Booklify.Domain.Enums;
using Booklify.Infrastructure.Models;
using Booklify.Infrastructure.Utils;
using System.Transactions;

namespace Booklify.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Background job for file upload operations
/// </summary>
public class FileUploadJob
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<FileUploadJob> _logger;

    public FileUploadJob(IServiceScopeFactory serviceScopeFactory, ILogger<FileUploadJob> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Execute file upload in background
    /// </summary>
    [Queue("file-operations")]
    public async Task ExecuteAsync(
        byte[] fileBytes, 
        string fileName, 
        string contentType, 
        string subDirectory, 
        string userId,
        FileUploadType uploadType = FileUploadType.None,
        Guid? entityId = null)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

        string? storagePath = null;
        Domain.Entities.FileInfo? fileInfo = null;

        try
        {
            // Upload file using storage service first (outside transaction)
            using var memoryStream = new MemoryStream(fileBytes);
            storagePath = await storageService.UploadFileAsync(memoryStream, fileName, contentType, subDirectory);

            // Begin transaction after successful file upload
            await unitOfWork.BeginTransactionAsync();

            // Create new FileInfo record with completed status
            fileInfo = new Domain.Entities.FileInfo
            {
                Name = fileName,
                MimeType = contentType,
                Extension = Path.GetExtension(fileName).TrimStart('.'),
                SizeKb = fileBytes.Length / 1024.0,
                Provider = storageService.GetType().Name,
                ServerUpload = storageService.GetType().Name,
                FilePath = storagePath,
                JobStatus = FileJobStatus.Completed,
                JobStartedAt = DateTime.UtcNow,
                JobCompletedAt = DateTime.UtcNow,
                CreatedBy = !string.IsNullOrEmpty(userId) ? Guid.Parse(userId) : null,
                CreatedAt = DateTime.UtcNow
            };

            await unitOfWork.FileInfoRepository.AddAsync(fileInfo);

            // Update entity with file reference if this is a specific upload type
            if (entityId.HasValue && uploadType != FileUploadType.None)
            {
                await UpdateEntityFile(unitOfWork, uploadType, entityId.Value, fileInfo);
            }

            // Commit all changes in one transaction
            await unitOfWork.CommitTransactionAsync();

            _logger.LogInformation(
                "File upload completed successfully: {FileName} -> {StoragePath} for {UploadType}", 
                fileName, storagePath, uploadType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error uploading file {FileName} for {UploadType}: {Message}", 
                fileName, uploadType, ex.Message);

            // Rollback transaction if it was started
            try
            {
                await unitOfWork.RollbackTransactionAsync();
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Failed to rollback transaction");
            }

            // Clean up the uploaded file if it exists
            if (!string.IsNullOrEmpty(storagePath))
            {
                try
                {
                    await storageService.DeleteFileAsync(storagePath);
                    _logger.LogInformation("Cleaned up file after failure: {FilePath}", storagePath);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "Failed to clean up file: {FilePath}", storagePath);
                }
            }

            throw;
        }
    }

    /// <summary>
    /// Update entity's file reference based on upload type
    /// </summary>
    private async Task UpdateEntityFile(IUnitOfWork unitOfWork, FileUploadType uploadType, Guid entityId, Domain.Entities.FileInfo fileInfo)
    {
        switch (uploadType)
        {
            case FileUploadType.Avatar:
                // Try to find in UserProfile first
                var userProfile = await unitOfWork.UserProfileRepository.GetFirstOrDefaultAsync(
                    u => u.Id == entityId);
                if (userProfile != null)
                {
                    userProfile.Avatar = fileInfo;
                    userProfile.AvatarId = fileInfo.Id;
                    await unitOfWork.UserProfileRepository.UpdateAsync(userProfile);
                    break;
                }

                // Try to find in StaffProfile
                var staffProfile = await unitOfWork.StaffProfileRepository.GetFirstOrDefaultAsync(
                    s => s.Id == entityId);
                if (staffProfile != null)
                {
                    staffProfile.Avatar = fileInfo;
                    staffProfile.AvatarId = fileInfo.Id;
                    await unitOfWork.StaffProfileRepository.UpdateAsync(staffProfile);
                }
                break;

            case FileUploadType.Book:
            case FileUploadType.Document:
            case FileUploadType.Epub:
                var book = await unitOfWork.BookRepository.GetByIdAsync(entityId);
                if (book != null)
                {
                    book.File = fileInfo;
                    book.FilePath = fileInfo.FilePath;
                    await unitOfWork.BookRepository.UpdateAsync(book);
                }
                break;

            case FileUploadType.BookCover:
                var bookForCover = await unitOfWork.BookRepository.GetByIdAsync(entityId);
                if (bookForCover != null)
                {
                    bookForCover.CoverImageUrl = fileInfo.FilePath;
                    await unitOfWork.BookRepository.UpdateAsync(bookForCover);
                }
                break;
        }
    }

    /// <summary>
    /// Execute batch file upload in background
    /// </summary>
    [Queue("file-operations")]
    public async Task ExecuteBatchAsync(List<FileUploadData> files, string subDirectory, string userId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

        _logger.LogInformation("Starting batch file upload for {Count} files", files.Count);

        var uploadedFiles = new List<Domain.Entities.FileInfo>();
        var failedFiles = new List<string>();

        foreach (var fileData in files)
        {
            try
            {
                using var memoryStream = new MemoryStream(fileData.FileBytes);
                var storagePath = await storageService.UploadFileAsync(memoryStream, fileData.FileName, fileData.ContentType, subDirectory);

                var newFileInfo = new Domain.Entities.FileInfo
                {
                    Id = Guid.NewGuid(),
                    Name = fileData.FileName,
                    FilePath = storagePath,
                    SizeKb = fileData.FileBytes.Length / 1024,
                    MimeType = fileData.ContentType,
                    Extension = Path.GetExtension(fileData.FileName).TrimStart('.'),
                    Provider = storageService.GetType().Name,
                    ServerUpload = storageService.GetType().Name,
                    JobStatus = FileJobStatus.Completed,
                    JobStartedAt = DateTime.UtcNow,
                    JobCompletedAt = DateTime.UtcNow,
                    CreatedBy = !string.IsNullOrEmpty(userId) ? Guid.Parse(userId) : null,
                    CreatedAt = DateTime.UtcNow
                };

                await unitOfWork.FileInfoRepository.AddAsync(newFileInfo);
                uploadedFiles.Add(newFileInfo);

                _logger.LogDebug("Successfully uploaded file: {FileName}", fileData.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file {FileName}: {Message}", fileData.FileName, ex.Message);
                failedFiles.Add(fileData.FileName);
            }
        }

        // Save all changes
        if (uploadedFiles.Any())
        {
            await unitOfWork.SaveChangesAsync();
        }

        _logger.LogInformation("Batch upload completed. Successful: {SuccessCount}, Failed: {FailedCount}", 
            uploadedFiles.Count, failedFiles.Count);

        if (failedFiles.Any())
        {
            throw new InvalidOperationException($"Failed to upload {failedFiles.Count} files: {string.Join(", ", failedFiles)}");
        }
    }
}