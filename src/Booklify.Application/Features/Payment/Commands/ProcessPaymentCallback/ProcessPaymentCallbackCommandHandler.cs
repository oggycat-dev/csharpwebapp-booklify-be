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

            // Note: Signature validation is already done in PaymentController IPN endpoint
            // No need to validate again here to avoid duplicate validation

            // Find payment record by VNPay TransactionId or by amount and timeframe
            // Since VNPay might return different TxnRef format than our GUID
            var payment = await FindPaymentRecordAsync(request, cancellationToken);

            if (payment == null)
            {
                _logger.LogWarning("Payment not found for OrderId: {OrderId}, TransactionId: {TransactionId}, Amount: {Amount}", 
                    request.OrderId, request.TransactionId, request.Amount);
                return Result<PaymentStatusResponse>.Failure("Payment not found", ErrorCode.NotFound);
            }

            // Check if payment was already processed
            if (payment.PaymentStatus == PaymentStatus.Success)
            {
                _logger.LogInformation("Payment already processed successfully for OrderId: {OrderId}", request.OrderId);
                var existingResponse = _mapper.Map<PaymentStatusResponse>(payment);
                return Result<PaymentStatusResponse>.Success(existingResponse, "Order already confirmed");
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

    private async Task<Domain.Entities.Payment?> FindPaymentRecordAsync(ProcessPaymentCallbackCommand request, CancellationToken cancellationToken)
    {
        // First try to find by amount and recent timeframe (more reliable)
        var recentPayments = await _context.Payments
            .Include(p => p.UserSubscription)
            .ThenInclude(us => us.Subscription)
            .Where(p => p.Amount == request.Amount && 
                        p.PaymentStatus == PaymentStatus.Pending &&
                        p.PaymentDate >= DateTime.UtcNow.AddMinutes(-30)) // Check for pending payments within the last 30 minutes
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(cancellationToken);

        if (recentPayments.Count > 0)
        {
            _logger.LogInformation("Found {Count} pending payment(s) with matching amount {Amount}", recentPayments.Count, request.Amount);
            return recentPayments.First(); // Return the most recent one
        }

        // Fallback: try to parse OrderId as GUID
        if (Guid.TryParse(request.OrderId, out var paymentId))
        {
            var payment = await _context.Payments
                .Include(p => p.UserSubscription)
                .ThenInclude(us => us.Subscription)
                .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

            if (payment != null)
            {
                _logger.LogInformation("Found payment by OrderId GUID: {PaymentId}", paymentId);
                return payment;
            }
        }

        _logger.LogWarning("Payment not found by any method for OrderId: {OrderId}, Amount: {Amount}", 
            request.OrderId, request.Amount);
        return null;
    }

    private async Task<bool> ActivateSubscriptionAsync(Domain.Entities.UserSubscription userSubscription, CancellationToken cancellationToken)
    {
        try
        {
            if (userSubscription.Status == EntityStatus.Active)
            {
                _logger.LogInformation("Subscription already active for UserSubscriptionId: {UserSubscriptionId}", userSubscription.Id);
                return true;
            }

            // Deactivate any existing active subscriptions for this user
            var existingActiveSubscriptions = await _context.UserSubscriptions
                .Where(us => us.UserId == userSubscription.UserId && 
                           us.Status == EntityStatus.Active && 
                           us.Id != userSubscription.Id &&
                           us.EndDate > DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingActiveSubscriptions)
            {
                existing.Status = EntityStatus.Inactive;
                _logger.LogInformation("Deactivated existing subscription: {SubscriptionId}", existing.Id);
            }

            // Activate the new subscription
            userSubscription.Status = EntityStatus.Active;
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