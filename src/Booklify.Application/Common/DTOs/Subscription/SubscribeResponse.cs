using System.Text.Json.Serialization;
using Booklify.Domain.Enums;

namespace Booklify.Application.Common.DTOs.Subscription;

/// <summary>
/// Response DTO for subscription creation with payment information
/// </summary>
public class SubscribeResponse
{
    [JsonPropertyName("user_subscription_id")]
    public Guid UserSubscriptionId { get; set; }
    
    [JsonPropertyName("payment_id")]
    public Guid PaymentId { get; set; }
    
    [JsonPropertyName("payment_url")]
    public string PaymentUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("payment_method")]
    public string PaymentMethod { get; set; } = string.Empty;
    
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "VND";
    
    [JsonPropertyName("expires_at")]
    public DateTime ExpiresAt { get; set; }
    
    [JsonPropertyName("subscription")]
    public SubscriptionResponse Subscription { get; set; } = new();
}

/// <summary>
/// Response DTO for payment status update
/// </summary>
public class PaymentStatusResponse
{
    [JsonPropertyName("payment_id")]
    public Guid PaymentId { get; set; }
    
    [JsonPropertyName("user_subscription_id")]
    public Guid UserSubscriptionId { get; set; }
    
    [JsonPropertyName("payment_status")]
    public PaymentStatus PaymentStatus { get; set; }
    
    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }
    
    [JsonPropertyName("payment_date")]
    public DateTime? PaymentDate { get; set; }
    
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "VND";
    
    [JsonPropertyName("payment_method")]
    public string? PaymentMethod { get; set; }
    
    [JsonPropertyName("bank_code")]
    public string? BankCode { get; set; }
    
    [JsonPropertyName("subscription_activated")]
    public bool SubscriptionActivated { get; set; }
    
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
    
    [JsonPropertyName("payment_status_text")]
    public string PaymentStatusText => PaymentStatus switch
    {
        PaymentStatus.Pending => "Đang xử lý",
        PaymentStatus.Success => "Thành công",
        PaymentStatus.Failed => "Thất bại",
        PaymentStatus.Cancelled => "Đã hủy",
        PaymentStatus.Refunded => "Đã hoàn tiền",
        PaymentStatus.Processing => "Đang xử lý",
        _ => "Không xác định"
    };
} 