using System.Text.Json.Serialization;
using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;

namespace Booklify.Application.Common.DTOs.Subscription;

/// <summary>
/// Filter model for subscription plans
/// </summary>
public class SubscriptionFilterModel : FilterBase
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("min_price")]
    public decimal? MinPrice { get; set; }
    
    [JsonPropertyName("max_price")]
    public decimal? MaxPrice { get; set; }
    
    [JsonPropertyName("min_duration")]
    public int? MinDuration { get; set; }
    
    [JsonPropertyName("max_duration")]
    public int? MaxDuration { get; set; }
    
    [JsonPropertyName("status")]
    public EntityStatus? Status { get; set; }
    
    [JsonPropertyName("is_popular")]
    public bool? IsPopular { get; set; }
} 