using AutoMapper;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Commons;
using Booklify.Domain.Entities;
using Booklify.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Booklify.Application.Features.User.Commands.ManageSubscription;

public class ManageSubscriptionCommandHandler : IRequestHandler<ManageSubscriptionCommand, Result<SubscriptionManagementResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ManageSubscriptionCommandHandler> _logger;

    public ManageSubscriptionCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<ManageSubscriptionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<SubscriptionManagementResponse>> Handle(ManageSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var isUserValid = await _currentUserService.IsUserValidAsync();
        if (!isUserValid)
        {
            return Result<SubscriptionManagementResponse>.Failure(
                "User is not authenticated",
                ErrorCode.Unauthorized);
        }

        var currentUserId = _currentUserService.UserId;

        try
        {
            await _unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            // Get user profile first to ensure user exists
            var userProfile = await _unitOfWork.UserProfileRepository
                .GetFirstOrDefaultAsync(x => x.Id == request.UserId);

            if (userProfile == null)
            {
                return Result<SubscriptionManagementResponse>.Failure(
                    "User not found",
                    ErrorCode.NotFound);
            }

            var response = new SubscriptionManagementResponse
            {
                Action = request.Request.Action,
                ProcessedAt = DateTime.UtcNow
            };

            UserSubscription? userSubscription = null;

            switch (request.Request.Action)
            {
                case SubscriptionAction.Extend:
                    userSubscription = await HandleExtendSubscription(userProfile, request.Request, currentUserId);
                    response.Message = "Subscription extended successfully";
                    break;

                case SubscriptionAction.Cancel:
                    userSubscription = await HandleCancelSubscription(userProfile, request.Request, currentUserId);
                    response.Message = "Subscription cancelled successfully";
                    break;

                case SubscriptionAction.Gift:
                    userSubscription = await HandleGiftSubscription(userProfile, request.Request, currentUserId);
                    response.Message = "Subscription gifted successfully";
                    break;

                case SubscriptionAction.ToggleAutoRenew:
                    userSubscription = await HandleToggleAutoRenew(userProfile, request.Request, currentUserId);
                    response.Message = "Auto-renew setting updated successfully";
                    break;

                case SubscriptionAction.ReSubscription:
                    userSubscription = await HandleReSubscription(userProfile, request.Request, currentUserId);
                    response.Message = "Re-subscription processed successfully";
                    break;

                default:
                    return Result<SubscriptionManagementResponse>.Failure(
                        "Invalid action",
                        ErrorCode.InvalidOperation);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            response.Success = true;
            response.Subscription = userSubscription != null ? _mapper.Map<UserSubscriptionResponse>(userSubscription) : null;

            return Result<SubscriptionManagementResponse>.Success(response);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error managing subscription for user ID: {UserId}, Action: {Action}", 
                request.UserId, request.Request.Action);
            return Result<SubscriptionManagementResponse>.Failure(
                "Error managing subscription",
                ErrorCode.InternalError);
        }
    }

    private async Task<UserSubscription> HandleExtendSubscription(UserProfile userProfile, SubscriptionManagementRequest request, string currentUserId)
    {
        var activeSubscription = await _unitOfWork.UserSubscriptionRepository
            .GetActiveSubscriptionByUserIdAsync(userProfile.Id);

        if (activeSubscription == null)
        {
            throw new InvalidOperationException("No active subscription found to extend");
        }

        if (!request.DurationDays.HasValue)
        {
            throw new ArgumentException("Duration days is required for extending subscription");
        }

        // Extend the end date
        activeSubscription.EndDate = activeSubscription.EndDate.AddDays(request.DurationDays.Value);
        BaseEntityExtensions.UpdateBaseEntity(activeSubscription, currentUserId);

        // Log the extension
        _logger.LogInformation("Extended subscription {SubscriptionId} for user {UserId} by {Days} days", 
            activeSubscription.Id, userProfile.Id, request.DurationDays.Value);

        return activeSubscription;
    }

    private async Task<UserSubscription> HandleCancelSubscription(UserProfile userProfile, SubscriptionManagementRequest request, string currentUserId)
    {
        var activeSubscription = await _unitOfWork.UserSubscriptionRepository
            .GetActiveSubscriptionByUserIdAsync(userProfile.Id);

        if (activeSubscription == null)
        {
            throw new InvalidOperationException("No active subscription found to cancel");
        }

        // Cancel the subscription (set end date to now)
        activeSubscription.EndDate = DateTime.UtcNow;
        activeSubscription.AutoRenew = false;
        BaseEntityExtensions.UpdateBaseEntity(activeSubscription, currentUserId);

        // Log the cancellation
        _logger.LogInformation("Cancelled subscription {SubscriptionId} for user {UserId}. Reason: {Reason}", 
            activeSubscription.Id, userProfile.Id, request.Reason);

        return activeSubscription;
    }

    private async Task<UserSubscription> HandleGiftSubscription(UserProfile userProfile, SubscriptionManagementRequest request, string currentUserId)
    {
        if (!request.SubscriptionId.HasValue)
        {
            throw new ArgumentException("Subscription ID is required for gifting");
        }

        if (!request.DurationDays.HasValue)
        {
            throw new ArgumentException("Duration days is required for gifting subscription");
        }

        // Get the subscription plan
        var subscription = await _unitOfWork.SubscriptionRepository
            .GetSubscriptionByIdAsync(request.SubscriptionId.Value);

        if (subscription == null)
        {
            throw new InvalidOperationException("Subscription plan not found");
        }

        // Create new user subscription
        var userSubscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userProfile.Id,
            SubscriptionId = request.SubscriptionId.Value,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(request.DurationDays.Value),
            AutoRenew = request.AutoRenew ?? false,
            Status = EntityStatus.Active
        };

        BaseEntityExtensions.InitializeBaseEntity(userSubscription, currentUserId);

        await _unitOfWork.UserSubscriptionRepository.AddAsync(userSubscription);

        // Log the gift
        _logger.LogInformation("Gifted subscription {SubscriptionId} to user {UserId} for {Days} days", 
            subscription.Id, userProfile.Id, request.DurationDays.Value);

        return userSubscription;
    }

    private async Task<UserSubscription> HandleToggleAutoRenew(UserProfile userProfile, SubscriptionManagementRequest request, string currentUserId)
    {
        var activeSubscription = await _unitOfWork.UserSubscriptionRepository
            .GetActiveSubscriptionByUserIdAsync(userProfile.Id);

        if (activeSubscription == null)
        {
            throw new InvalidOperationException("No active subscription found to toggle auto-renew");
        }

        // Toggle auto-renew
        activeSubscription.AutoRenew = request.AutoRenew ?? !activeSubscription.AutoRenew;
        BaseEntityExtensions.UpdateBaseEntity(activeSubscription, currentUserId);

        // Log the toggle
        _logger.LogInformation("Toggled auto-renew for subscription {SubscriptionId} of user {UserId} to {AutoRenew}", 
            activeSubscription.Id, userProfile.Id, activeSubscription.AutoRenew);

        return activeSubscription;
    }

    private async Task<UserSubscription> HandleReSubscription(UserProfile userProfile, SubscriptionManagementRequest request, string currentUserId)
    {
        if (!request.SubscriptionId.HasValue)
        {
            throw new ArgumentException("Subscription ID is required for re-subscription");
        }

        if (!request.DurationDays.HasValue)
        {
            throw new ArgumentException("Duration days is required for re-subscription");
        }

        if (string.IsNullOrWhiteSpace(request.PaymentProofUrl))
        {
            throw new ArgumentException("Payment proof URL is required for re-subscription");
        }

        // Get the subscription plan
        var subscription = await _unitOfWork.SubscriptionRepository
            .GetSubscriptionByIdAsync(request.SubscriptionId.Value);

        if (subscription == null)
        {
            throw new InvalidOperationException("Subscription plan not found");
        }

        // Create new user subscription for re-subscription
        var userSubscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userProfile.Id,
            SubscriptionId = request.SubscriptionId.Value,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(request.DurationDays.Value),
            AutoRenew = request.AutoRenew ?? false,
            Status = EntityStatus.Active
        };

        BaseEntityExtensions.InitializeBaseEntity(userSubscription, currentUserId);

        await _unitOfWork.UserSubscriptionRepository.AddAsync(userSubscription);

        // Log the re-subscription with payment proof
        _logger.LogInformation("Re-subscribed user {UserId} to subscription {SubscriptionId} for {Days} days. Payment proof: {PaymentProof}", 
            userProfile.Id, subscription.Id, request.DurationDays.Value, request.PaymentProofUrl);

        return userSubscription;
    }
} 