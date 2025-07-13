using System.Text.Json.Serialization;
using Booklify.Domain.Enums;

namespace Booklify.Application.Common.DTOs.Payment;

/// <summary>
/// Response DTO for payment history
/// </summary>
public class PaymentHistoryResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    
    [JsonPropertyName("payment_method")]
    public string PaymentMethod { get; set; } = string.Empty;
    
    [JsonPropertyName("payment_status")]
    public string PaymentStatus { get; set; } = string.Empty;
    
    [JsonPropertyName("payment_date")]
    public DateTime PaymentDate { get; set; }
    
    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "VND";
    
    [JsonPropertyName("subscription_name")]
    public string? SubscriptionName { get; set; }
    
    [JsonPropertyName("subscription_duration")]
    public int? SubscriptionDuration { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("modified_at")]
    public DateTime? ModifiedAt { get; set; }
} 