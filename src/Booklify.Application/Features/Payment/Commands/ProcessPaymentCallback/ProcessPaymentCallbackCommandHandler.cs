using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;

namespace Booklify.Application.Features.Payment.Commands.ProcessPaymentCallback;

public class ProcessPaymentCallbackCommandHandler : IRequestHandler<ProcessPaymentCallbackCommand, Result<PaymentStatusResponse>>
{
    private readonly IBooklifyDbContext _context;
    private readonly IVNPayService _vnPayService;
    private readonly IMapper _mapper;
    private readonly ILogger<ProcessPaymentCallbackCommandHandler> _logger;

    public ProcessPaymentCallbackCommandHandler(
        IBooklifyDbContext context,
        IVNPayService vnPayService,
        IMapper mapper,
        ILogger<ProcessPaymentCallbackCommandHandler> logger)
    {
        _context = context;
        _vnPayService = vnPayService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<PaymentStatusResponse>> Handle(ProcessPaymentCallbackCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing VNPay callback for OrderId: {OrderId}, TransactionId: {TransactionId}", 
                request.OrderId, request.TransactionId);

            // Validate VNPay signature
            var isValidSignature = await _vnPayService.ValidateCallbackAsync(request.AdditionalData, request.SecureHash);
            if (!isValidSignature)
            {
                _logger.LogWarning("Invalid VNPay signature for OrderId: {OrderId}", request.OrderId);
                return Result<PaymentStatusResponse>.Failure("Invalid payment signature", ErrorCode.Forbidden);
            }

            // Find payment record by OrderId (which is our PaymentId)
            if (!Guid.TryParse(request.OrderId, out var paymentId))
            {
                _logger.LogWarning("Invalid OrderId format: {OrderId}", request.OrderId);
                return Result<PaymentStatusResponse>.Failure("Invalid order ID format", ErrorCode.ValidationFailed);
            }

            var payment = await _context.Payments
                .Include(p => p.UserSubscription)
                .ThenInclude(us => us.Subscription)
                .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

            if (payment == null)
            {
                _logger.LogWarning("Payment not found for OrderId: {OrderId}", request.OrderId);
                return Result<PaymentStatusResponse>.Failure("Payment not found", ErrorCode.NotFound);
            }

            // Check if payment was already processed
            if (payment.PaymentStatus == PaymentStatus.Success)
            {
                _logger.LogInformation("Payment already processed successfully for OrderId: {OrderId}", request.OrderId);
                var existingResponse = _mapper.Map<PaymentStatusResponse>(payment);
                return Result<PaymentStatusResponse>.Success(existingResponse);
            }

            // Validate amount
            if (Math.Abs(payment.Amount - request.Amount) > 0.01m)
            {
                _logger.LogWarning("Amount mismatch for OrderId: {OrderId}. Expected: {Expected}, Received: {Received}", 
                    request.OrderId, payment.Amount, request.Amount);
                
                // Update payment status to failed
                payment.PaymentStatus = PaymentStatus.Failed;
                payment.ProviderResponse = $"Amount mismatch - Expected: {payment.Amount}, Received: {request.Amount}";
                await _context.SaveChangesAsync(cancellationToken);
                
                return Result<PaymentStatusResponse>.Failure("Payment amount mismatch", ErrorCode.ValidationFailed);
            }

            // Process payment based on response code and transaction status (VNPay standard)
            PaymentStatus newStatus;
            bool subscriptionActivated = false;

            // Check both response code and transaction status for success (following VNPay standard)
            if (request.ResponseCode == "00" && request.TransactionStatus == "00")
            {
                // Payment successful
                newStatus = PaymentStatus.Success;
                subscriptionActivated = await ActivateSubscriptionAsync(payment.UserSubscription, cancellationToken);
                _logger.LogInformation("Payment successful for OrderId: {OrderId}, TransactionId: {TransactionId}", 
                    request.OrderId, request.TransactionId);
            }
            else if (request.ResponseCode == "24")
            {
                // Transaction cancelled by user
                newStatus = PaymentStatus.Cancelled;
                _logger.LogInformation("Payment cancelled by user for OrderId: {OrderId}", request.OrderId);
            }
            else
            {
                // Payment failed
                newStatus = PaymentStatus.Failed;
                _logger.LogWarning("Payment failed for OrderId: {OrderId}, ResponseCode: {ResponseCode}, TransactionStatus: {TransactionStatus}", 
                    request.OrderId, request.ResponseCode, request.TransactionStatus);
            }

            // Update payment record
            payment.PaymentStatus = newStatus;
            payment.TransactionId = request.TransactionId;
            payment.PaymentDate = ParseVNPayDate(request.PayDate);
            payment.ProviderResponse = System.Text.Json.JsonSerializer.Serialize(new
            {
                response_code = request.ResponseCode,
                transaction_status = request.TransactionStatus,
                transaction_id = request.TransactionId,
                bank_code = request.BankCode,
                payment_method = request.PaymentMethod,
                pay_date = request.PayDate
            });

            await _context.SaveChangesAsync(cancellationToken);

            var response = new PaymentStatusResponse
            {
                PaymentId = payment.Id,
                UserSubscriptionId = payment.UserSubscriptionId,
                PaymentStatus = payment.PaymentStatus,
                TransactionId = payment.TransactionId,
                PaymentDate = payment.PaymentDate,
                Amount = payment.Amount,
                SubscriptionActivated = subscriptionActivated
            };

            _logger.LogInformation("Payment callback processed successfully for OrderId: {OrderId}, Status: {Status}", 
                request.OrderId, newStatus);

            return Result<PaymentStatusResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment callback for OrderId: {OrderId}", request.OrderId);
            return Result<PaymentStatusResponse>.Failure("Internal error processing payment callback", ErrorCode.InternalError);
        }
    }

    private async Task<bool> ActivateSubscriptionAsync(Domain.Entities.UserSubscription userSubscription, CancellationToken cancellationToken)
    {
        try
        {
            if (userSubscription.IsActive)
            {
                _logger.LogInformation("Subscription already active for UserSubscriptionId: {UserSubscriptionId}", userSubscription.Id);
                return true;
            }

            // Deactivate any existing active subscriptions for this user
            var existingActiveSubscriptions = await _context.UserSubscriptions
                .Where(us => us.UserId == userSubscription.UserId && 
                           us.IsActive && 
                           us.Id != userSubscription.Id &&
                           us.Status == EntityStatus.Active)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingActiveSubscriptions)
            {
                existing.IsActive = false;
                _logger.LogInformation("Deactivated existing subscription: {SubscriptionId}", existing.Id);
            }

            // Activate the new subscription
            userSubscription.IsActive = true;
            userSubscription.StartDate = DateTime.UtcNow;
            userSubscription.EndDate = DateTime.UtcNow.AddDays(userSubscription.Subscription.Duration);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Activated subscription for UserSubscriptionId: {UserSubscriptionId}, EndDate: {EndDate}", 
                userSubscription.Id, userSubscription.EndDate);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating subscription for UserSubscriptionId: {UserSubscriptionId}", userSubscription.Id);
            return false;
        }
    }

    private DateTime ParseVNPayDate(string vnPayDate)
    {
        try
        {
            // VNPay date format: yyyyMMddHHmmss
            if (DateTime.TryParseExact(vnPayDate, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out var result))
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse VNPay date: {VNPayDate}", vnPayDate);
        }

        return DateTime.UtcNow;
    }
} 