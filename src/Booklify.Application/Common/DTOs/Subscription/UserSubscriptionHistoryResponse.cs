using System.Text.Json.Serialization;
using Booklify.Domain.Enums;

namespace Booklify.Application.Common.DTOs.Subscription;

/// <summary>
/// Response DTO for user subscription history
/// </summary>
public class UserSubscriptionHistoryResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("subscription_name")]
    public string SubscriptionName { get; set; } = string.Empty;
    
    [JsonPropertyName("subscription_description")]
    public string SubscriptionDescription { get; set; } = string.Empty;
    
    [JsonPropertyName("start_date")]
    public DateTime StartDate { get; set; }
    
    [JsonPropertyName("end_date")]
    public DateTime EndDate { get; set; }
    
    [JsonPropertyName("auto_renew")]
    public bool AutoRenew { get; set; }
    
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
    
    [JsonPropertyName("duration_days")]
    public int DurationDays { get; set; }
    
    [JsonPropertyName("status")]
    public EntityStatus Status { get; set; }
    
    [JsonPropertyName("status_string")]
    public string StatusString { get; set; } = string.Empty;
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("modified_at")]
    public DateTime? ModifiedAt { get; set; }
} 