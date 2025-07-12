using AutoMapper;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Commons;
using Booklify.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Transactions;

namespace Booklify.Application.Features.User.Commands.UpdateProfile;

public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateUserProfileCommandHandler> _logger;
    private readonly IFileService _fileService;
    private readonly IStorageService _storageService;
    private readonly IFileBackgroundService _fileBackgroundService;

    public UpdateUserProfileCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<UpdateUserProfileCommandHandler> logger,
        IFileService fileService,
        IStorageService storageService,
        IFileBackgroundService fileBackgroundService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
        _fileService = fileService;
        _storageService = storageService;
        _fileBackgroundService = fileBackgroundService;
    }

    public async Task<Result> Handle(UpdateUserProfileCommand command, CancellationToken cancellationToken)
    {
        var isUserValid = await _currentUserService.IsUserValidAsync();
        if (!isUserValid)
        {
            return Result.Failure(
                "User is not authenticated",
                ErrorCode.Unauthorized);
        }

        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure(
                "User ID not found",
                ErrorCode.Unauthorized);
        }

        Domain.Entities.UserProfile? existingProfile;
        // 1. Find existing user profile with IdentityUser
        existingProfile = await _unitOfWork.UserProfileRepository
            .GetFirstOrDefaultAsync(
                x => x.IdentityUserId == userId,
                x => x.IdentityUser,
                x => x.Avatar);

        if (existingProfile == null)
        {
            return Result.Failure(
                "User profile not found",
                ErrorCode.NotFound);
        }

        try
        {
            // Store old file info for background deletion
            Domain.Entities.FileInfo? oldAvatar = null;
            string? oldFilePath = null;
            Guid? oldFileId = null;

            if (command.Request.Avatar != null && existingProfile.Avatar != null)
            {
                oldAvatar = existingProfile.Avatar;
                oldFilePath = oldAvatar.FilePath;
                oldFileId = oldAvatar.Id;
            }

            using (var scope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted,
                    Timeout = TimeSpan.FromMinutes(1)
                },
                TransactionScopeAsyncFlowOption.Enabled
            ))
            {
                // 2. Apply partial updates (only update fields that are provided)
                var request = command.Request;
                bool hasChanges = false;

                if (!string.IsNullOrWhiteSpace(request.FirstName) && request.FirstName != existingProfile.FirstName)
                {
                    existingProfile.FirstName = request.FirstName;
                    hasChanges = true;
                }

                if (!string.IsNullOrWhiteSpace(request.LastName) && request.LastName != existingProfile.LastName)
                {
                    existingProfile.LastName = request.LastName;
                    hasChanges = true;
                }

                // Update FullName if FirstName or LastName changed
                if (hasChanges)
                {
                    existingProfile.FullName = $"{existingProfile.FirstName} {existingProfile.LastName}";
                }

                if (!string.IsNullOrWhiteSpace(request.Phone) && request.Phone != existingProfile.Phone)
                {
                    // Check if phone already exists
                    var phoneExists = await _unitOfWork.UserProfileRepository
                        .AnyAsync(x => x.Phone == request.Phone && x.Id != existingProfile.Id);
                    if (phoneExists)
                    {
                        return Result.Failure(
                            "Phone number already exists",
                            ErrorCode.ValidationFailed);
                    }

                    existingProfile.Phone = request.Phone;
                    hasChanges = true;
                }

                if (!string.IsNullOrWhiteSpace(request.Address) && request.Address != existingProfile.Address)
                {
                    existingProfile.Address = request.Address;
                    hasChanges = true;
                }

                if (request.Birthday.HasValue && request.Birthday != existingProfile.Birthday)
                {
                    existingProfile.Birthday = request.Birthday;
                    hasChanges = true;
                }

                if (request.Gender.HasValue && request.Gender != existingProfile.Gender)
                {
                    existingProfile.Gender = request.Gender;
                    hasChanges = true;
                }

                // Handle avatar upload
                if (request.Avatar != null)
                {
                    // Upload new avatar
                    var uploadResult = await _fileService.UploadFileAsync(
                        request.Avatar,
                        "avatars",
                        userId);

                    if (!uploadResult.IsSuccess)
                    {
                        return Result.Failure(
                            "Failed to upload avatar",
                            ErrorCode.InternalError);
                    }

                    // Create or update FileInfo
                    var fileInfo = existingProfile.Avatar ?? new Domain.Entities.FileInfo();
                    fileInfo.Name = uploadResult.Data.OriginalFileName;
                    fileInfo.FilePath = uploadResult.Data.FilePath;
                    fileInfo.SizeKb = uploadResult.Data.SizeKb;
                    fileInfo.MimeType = uploadResult.Data.MimeType;
                    fileInfo.Extension = uploadResult.Data.Extension;
                    fileInfo.ServerUpload = _storageService.GetType().Name;
                    fileInfo.Provider = _storageService.GetType().Name;

                    if (existingProfile.Avatar == null)
                    {
                        // Initialize new FileInfo
                        BaseEntityExtensions.InitializeBaseEntity(fileInfo, userId);
                        var savedFileInfo = await _unitOfWork.FileInfoRepository.AddAsync(fileInfo);
                        existingProfile.Avatar = savedFileInfo;
                        existingProfile.AvatarId = savedFileInfo.Id;
                    }
                    else
                    {
                        // Update existing FileInfo
                        BaseEntityExtensions.UpdateBaseEntity(fileInfo, userId);
                        await _unitOfWork.FileInfoRepository.UpdateAsync(fileInfo);
                    }

                    hasChanges = true;
                }

                if (!hasChanges)
                {
                    return Result.Success("No changes detected");
                }

                // Update base entity fields
                BaseEntityExtensions.UpdateBaseEntity(existingProfile, userId);

                // Save changes
                await _unitOfWork.UserProfileRepository.UpdateAsync(existingProfile);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Complete transaction
                scope.Complete();

                // Queue background jobs for file cleanup AFTER successful transaction
                if (oldAvatar != null && oldFileId.HasValue)
                {
                    try
                    {
                        // Queue physical file deletion in background
                        if (!string.IsNullOrEmpty(oldFilePath))
                        {
                            _fileBackgroundService.QueueFileDelete(oldFilePath, userId, oldFileId);
                            _logger.LogInformation("Queued old avatar file deletion: {FilePath}", oldFilePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but don't throw - file cleanup is not critical
                        _logger.LogError(ex, "Error queueing avatar file cleanup for user {UserId}", userId);
                    }
                }
            }

            return Result.Success("Profile updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile for user ID: {UserId}", userId);
            return Result.Failure(
                "Error updating user profile",
                ErrorCode.InternalError);
        }
    }
} 