using MediatR;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Entities;
using Booklify.Domain.Enums;

namespace Booklify.Application.Features.Subscription.Commands.Subscribe;

public class SubscribeCommandHandler : IRequestHandler<SubscribeCommand, Result<SubscribeResponse>>
{
    private readonly IBooklifyDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IVNPayService _vnPayService;
    private readonly IMapper _mapper;

    public SubscribeCommandHandler(
        IBooklifyDbContext context,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IVNPayService vnPayService,
        IMapper mapper)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _vnPayService = vnPayService;
        _mapper = mapper;
    }

    public async Task<Result<SubscribeResponse>> Handle(SubscribeCommand request, CancellationToken cancellationToken)
    {
        // Get current user
        var currentUserId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Result<SubscribeResponse>.Failure("User is not authenticated", ErrorCode.Unauthorized);
        }

        // Find user profile
        var userProfile = await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.IdentityUserId == currentUserId, cancellationToken);

        if (userProfile == null)
        {
            return Result<SubscribeResponse>.Failure("User profile not found", ErrorCode.UserNotFound);
        }

        // Find subscription plan
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == request.Request.SubscriptionId && s.Status == EntityStatus.Active, cancellationToken);

        if (subscription == null)
        {
            return Result<SubscribeResponse>.Failure("Subscription plan not found", ErrorCode.NotFound);
        }

        // Check if user already has an active subscription
        var existingSubscription = await _context.UserSubscriptions
            .FirstOrDefaultAsync(us => us.UserId == userProfile.Id && us.IsActive && us.Status == EntityStatus.Active, cancellationToken);

        if (existingSubscription != null)
        {
            return Result<SubscribeResponse>.Failure("User already has an active subscription", ErrorCode.BusinessRuleViolation);
        }

        // Create UserSubscription (initially inactive until payment is completed)
        var userSubscription = new UserSubscription
        {
            UserId = userProfile.Id,
            SubscriptionId = subscription.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(subscription.Duration),
            IsActive = false, // Will be activated after successful payment
            AutoRenew = request.Request.AutoRenew,
            Status = EntityStatus.Active
        };

        _context.UserSubscriptions.Add(userSubscription);
        await _context.SaveChangesAsync(cancellationToken);

        // Create Payment record
        var payment = new Domain.Entities.Payment
        {
            UserSubscriptionId = userSubscription.Id,
            Amount = subscription.Price,
            PaymentMethod = request.Request.PaymentMethod,
            PaymentStatus = PaymentStatus.Pending,
            PaymentDate = DateTime.UtcNow,
            Currency = "VND",
            Description = $"Subscription payment for {subscription.Name}"
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        // Create VNPay payment URL
        var vnPayRequest = new Application.Common.DTOs.Payment.VNPayPaymentRequest
        {
            OrderId = payment.Id.ToString(),
            Amount = subscription.Price,
            OrderDescription = $"Payment for subscription: {subscription.Name}",
            ReturnUrl = null, // Will use default from settings
            Language = "vn"
        };

        var vnPayResponse = await _vnPayService.CreatePaymentUrlAsync(vnPayRequest, "127.0.0.1");

        if (!vnPayResponse.Success || string.IsNullOrEmpty(vnPayResponse.PaymentUrl))
        {
            var errorMessage = !string.IsNullOrEmpty(vnPayResponse.ErrorMessage) 
                ? vnPayResponse.ErrorMessage 
                : "Failed to create payment URL";
            return Result<SubscribeResponse>.Failure(errorMessage, ErrorCode.ExternalServiceError);
        }

        // Update payment with transaction info
        payment.TransactionId = $"SUB_{payment.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        await _context.SaveChangesAsync(cancellationToken);

        var response = new SubscribeResponse
        {
            UserSubscriptionId = userSubscription.Id,
            PaymentId = payment.Id,
            PaymentUrl = vnPayResponse.PaymentUrl,
            PaymentMethod = payment.PaymentMethod,
            Amount = payment.Amount,
            Currency = payment.Currency,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15), // VNPay URL expires in 15 minutes
            Subscription = _mapper.Map<SubscriptionResponse>(subscription)
        };

        return Result<SubscribeResponse>.Success(response);
    }
} 