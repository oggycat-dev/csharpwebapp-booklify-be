using MediatR;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Booklify.Application.Common.DTOs.User;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.Interfaces;

namespace Booklify.Application.Features.User.Queries.GetUserById;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDetailResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetUserByIdQueryHandler> _logger;
    private readonly IFileService _fileService;
    
    public GetUserByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetUserByIdQueryHandler> logger,
        IFileService fileService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _fileService = fileService;
    }
    
    public async Task<Result<UserDetailResponse>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get user by ID with all related data
            var user = await _unitOfWork.UserProfileRepository.GetUserByIdAsync(request.Id);
            
            if (user == null)
            {
                return Result<UserDetailResponse>.Failure("User not found", ErrorCode.NotFound);
            }
            
            // Map to UserDetailResponse
            var userDetailResponse = _mapper.Map<UserDetailResponse>(user);
            
            // Set avatar URL if user has avatar
            if (user.Avatar?.FilePath != null)
            {
                userDetailResponse.AvatarUrl = _fileService.GetFileUrl(user.Avatar.FilePath);
            }
            
            // Set username and email from IdentityUser
            if (user.IdentityUser != null)
            {
                userDetailResponse.Username = user.IdentityUser.UserName ?? string.Empty;
                userDetailResponse.Email = user.IdentityUser.Email ?? string.Empty;
                userDetailResponse.IsActive = user.IdentityUser.IsActive;
            }
            
            // Find current active subscription
            var activeSubscription = user.UserSubscriptions?
                .Where(us => us.IsActive && us.EndDate > DateTime.UtcNow)
                .OrderByDescending(us => us.EndDate)
                .FirstOrDefault();
            
            if (activeSubscription != null)
            {
                userDetailResponse.Subscription = _mapper.Map<Booklify.Application.Common.DTOs.Subscription.UserSubscriptionResponse>(activeSubscription);
                userDetailResponse.HasActiveSubscription = true;
            }
            else
            {
                userDetailResponse.HasActiveSubscription = false;
            }
            
            return Result<UserDetailResponse>.Success(userDetailResponse, "User details retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {UserId}", request.Id);
            return Result<UserDetailResponse>.Failure("An error occurred while retrieving user details", ErrorCode.InternalError);
        }
    }
} 