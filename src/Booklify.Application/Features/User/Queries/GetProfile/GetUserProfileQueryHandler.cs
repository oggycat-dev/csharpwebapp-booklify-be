using AutoMapper;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.DTOs.User;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booklify.Application.Features.User.Queries.GetProfile;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, Result<UserDetailResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetUserProfileQueryHandler> _logger;
    private readonly IFileService _fileService;

    public GetUserProfileQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<GetUserProfileQueryHandler> logger,
        IFileService fileService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
        _fileService = fileService;
    }

    public async Task<Result<UserDetailResponse>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var isUserValid = await _currentUserService.IsUserValidAsync();
        if (!isUserValid)
        {
            return Result<UserDetailResponse>.Failure(
                "User is not authenticated",
                ErrorCode.Unauthorized);
        }

        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result<UserDetailResponse>.Failure(
                "User ID not found",
                ErrorCode.Unauthorized);
        }

        try
        {
            // Get user profile with IdentityUser and UserSubscriptions
            var userProfile = await _unitOfWork.UserProfileRepository
                .GetFirstOrDefaultAsync(
                    x => x.IdentityUserId == userId,
                    x => x.IdentityUser,
                    x => x.UserSubscriptions,
                    x => x.Avatar);

            if (userProfile == null)
            {
                return Result<UserDetailResponse>.Failure(
                    "User profile not found",
                    ErrorCode.NotFound);
            }

            // Map to response
            var response = _mapper.Map<UserDetailResponse>(userProfile);
            
            // Set additional fields from IdentityUser
            if (userProfile.IdentityUser != null)
            {
                response.Email = userProfile.IdentityUser.Email ?? string.Empty;
                response.Username = userProfile.IdentityUser.UserName ?? string.Empty;
                response.IsActive = userProfile.IdentityUser.IsActive;
            }

            // Set subscription info
            var activeSubscription = userProfile.UserSubscriptions
                ?.OrderByDescending(x => x.EndDate)
                .FirstOrDefault(x => x.EndDate >= DateTime.UtcNow);
                
            response.HasActiveSubscription = activeSubscription != null;
            response.Subscription = activeSubscription != null ? 
                _mapper.Map<UserSubscriptionResponse>(activeSubscription) : null;

            // Set avatar URL
            if (userProfile.Avatar?.FilePath != null)
            {
                response.AvatarUrl = _fileService.GetFileUrl(userProfile.Avatar.FilePath);
            }

            return Result<UserDetailResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile for user ID: {UserId}", userId);
            return Result<UserDetailResponse>.Failure(
                "Error getting user profile",
                ErrorCode.InternalError);
        }
    }
} 