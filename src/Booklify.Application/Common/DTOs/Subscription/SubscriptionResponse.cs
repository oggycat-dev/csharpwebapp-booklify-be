using System.Text.Json.Serialization;
using Booklify.Domain.Enums;

namespace Booklify.Application.Common.DTOs.Subscription;

/// <summary>
/// Response DTO for subscription information
/// </summary>
public class SubscriptionResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
    
    [JsonPropertyName("duration")]
    public int Duration { get; set; }
    
    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = new();
    
    [JsonPropertyName("is_popular")]
    public bool IsPopular { get; set; }
    
    [JsonPropertyName("display_order")]
    public int DisplayOrder { get; set; }
    
    [JsonPropertyName("status")]
    public EntityStatus Status { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response DTO for user subscription information
/// </summary>
public class UserSubscriptionResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }
    
    [JsonPropertyName("subscription")]
    public SubscriptionResponse Subscription { get; set; } = new();
    
    [JsonPropertyName("start_date")]
    public DateTime StartDate { get; set; }
    
    [JsonPropertyName("end_date")]
    public DateTime EndDate { get; set; }
    
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
    
    [JsonPropertyName("auto_renew")]
    public bool AutoRenew { get; set; }
    
    [JsonPropertyName("status")]
    public EntityStatus Status { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request DTO for subscribing to a plan
/// </summary>
public class SubscribeRequest
{
    [JsonPropertyName("subscription_id")]
    public Guid SubscriptionId { get; set; }
    
    [JsonPropertyName("auto_renew")]
    public bool AutoRenew { get; set; } = false;
    
    [JsonPropertyName("payment_method")]
    public string PaymentMethod { get; set; } = "VNPay";
} 