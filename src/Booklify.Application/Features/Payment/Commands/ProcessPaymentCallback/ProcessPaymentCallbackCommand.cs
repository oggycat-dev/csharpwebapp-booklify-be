using MediatR;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Payment.Commands.ProcessPaymentCallback;

/// <summary>
/// Command to process VNPay payment callback
/// </summary>
public record ProcessPaymentCallbackCommand : IRequest<Result<PaymentStatusResponse>>
{
    public string TransactionId { get; init; } = string.Empty;
    public string OrderId { get; init; } = string.Empty;
    public string ResponseCode { get; init; } = string.Empty;
    public string TransactionStatus { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string BankCode { get; init; } = string.Empty;
    public string PayDate { get; init; } = string.Empty;
    public string SecureHash { get; init; } = string.Empty;
    public Dictionary<string, string> AdditionalData { get; init; } = new();
} 