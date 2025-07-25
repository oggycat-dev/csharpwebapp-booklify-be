using MediatR;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.DTOs.User;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.Interfaces;

namespace Booklify.Application.Features.User.Commands.UpdateUserStatus;

public class UpdateUserStatusCommandHandler : IRequestHandler<UpdateUserStatusCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;
    private readonly ILogger<UpdateUserStatusCommandHandler> _logger;
    
    public UpdateUserStatusCommandHandler(
        IUnitOfWork unitOfWork,
        IIdentityService identityService,
        ILogger<UpdateUserStatusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _identityService = identityService;
        _logger = logger;
    }
    
    public async Task<Result> Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get user profile first
            var userProfile = await _unitOfWork.UserProfileRepository.GetFirstOrDefaultAsync(u => u.Id == request.UserId);
            if (userProfile == null)
            {
                return Result.Failure("User not found", ErrorCode.NotFound);
            }
            
            // Get identity user
            var identityUserResult = await _identityService.FindByIdAsync(userProfile.IdentityUserId ?? string.Empty);
            if (!identityUserResult.IsSuccess || identityUserResult.Data == null)
            {
                return Result.Failure("Identity user not found", ErrorCode.NotFound);
            }
            
            var identityUser = identityUserResult.Data;
            
            // Check if status is already the same
            if (identityUser.IsActive == request.Request.IsActive)
            {
                return Result.Failure(
                    $"User account is already {(request.Request.IsActive ? "active" : "inactive")}", 
                    ErrorCode.ValidationFailed);
            }
            
            // Update identity user status
            identityUser.IsActive = request.Request.IsActive;
            var updateResult = await _identityService.UpdateUserAsync(identityUser);
            
            if (!updateResult.IsSuccess)
            {
                return Result.Failure("Failed to update user status", ErrorCode.InternalError);
            }
            
            _logger.LogInformation("User status updated successfully. UserId: {UserId}, IsActive: {IsActive}", 
                request.UserId, request.Request.IsActive);
            
            var statusMessage = $"User account {(request.Request.IsActive ? "activated" : "deactivated")} successfully. Current status: {(request.Request.IsActive ? "Active" : "Inactive")}";
            return Result.Success(statusMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user status. UserId: {UserId}", request.UserId);
            return Result.Failure("An error occurred while updating user status", ErrorCode.InternalError);
        }
    }
} 