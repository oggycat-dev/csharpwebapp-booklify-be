using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Booklify.Application.Common.DTOs.Subscription;

/// <summary>
/// Request DTO for managing subscription (extend, cancel, gift, toggle auto-renew)
/// </summary>
public class SubscriptionManagementRequest
{
    [JsonPropertyName("action")]
    [Required(ErrorMessage = "Action is required")]
    public SubscriptionAction Action { get; set; }
    
    [JsonPropertyName("subscription_id")]
    public Guid? SubscriptionId { get; set; }
    
    [JsonPropertyName("duration_days")]
    [Range(1, 365, ErrorMessage = "Duration must be between 1 and 365 days")]
    public int? DurationDays { get; set; }
    
    [JsonPropertyName("reason")]
    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string? Reason { get; set; }
    
    [JsonPropertyName("payment_proof_url")]
    [MaxLength(2048, ErrorMessage = "Payment proof URL cannot exceed 2048 characters")]
    public string? PaymentProofUrl { get; set; }
    
    [JsonPropertyName("payment_method")]
    [MaxLength(50, ErrorMessage = "Payment method cannot exceed 50 characters")]
    public string? PaymentMethod { get; set; }
    
    [JsonPropertyName("payment_amount")]
    [Range(0, double.MaxValue, ErrorMessage = "Payment amount must be positive")]
    public decimal? PaymentAmount { get; set; }
    
    [JsonPropertyName("auto_renew")]
    public bool? AutoRenew { get; set; }
}

/// <summary>
/// Enumeration for subscription management actions
/// </summary>
public enum SubscriptionAction
{
    /// <summary>
    /// Extend existing subscription
    /// </summary>
    Extend,
    
    /// <summary>
    /// Cancel existing subscription
    /// </summary>
    Cancel,
    
    /// <summary>
    /// Gift subscription to user
    /// </summary>
    Gift,
    
    /// <summary>
    /// Toggle auto-renewal setting
    /// </summary>
    ToggleAutoRenew,
    
    /// <summary>
    /// Re-activate subscription with payment proof
    /// </summary>
    ReSubscription
} 