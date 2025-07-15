using System.Text.Json.Serialization;

namespace Booklify.Application.Common.DTOs.Subscription;

/// <summary>
/// Response DTO for subscription management operations
/// </summary>
public class SubscriptionManagementResponse
{
    [JsonPropertyName("action")]
    public SubscriptionAction Action { get; set; }
    
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("subscription")]
    public UserSubscriptionResponse? Subscription { get; set; }
    
    [JsonPropertyName("processed_at")]
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
} 