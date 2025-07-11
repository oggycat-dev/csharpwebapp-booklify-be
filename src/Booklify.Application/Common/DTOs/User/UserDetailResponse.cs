using System.Text.Json.Serialization;
using Booklify.Application.Common.DTOs.Subscription;

namespace Booklify.Application.Common.DTOs.User;

public class UserDetailResponse : UserResponse
{
    [JsonPropertyName("birthday")]
    public DateTime? Birthday { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("subscription")]
    public UserSubscriptionResponse? Subscription { get; set; }

    [JsonPropertyName("has_active_subscription")]
    public bool HasActiveSubscription { get; set; }
} 