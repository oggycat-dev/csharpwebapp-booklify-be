using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Booklify.Domain.Enums;

namespace Booklify.Application.Common.DTOs.Subscription;

/// <summary>
/// Request DTO for updating a subscription plan
/// </summary>
public class UpdateSubscriptionRequest
{
    [JsonPropertyName("name")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Subscription name must be between 3 and 100 characters")]
    public string? Name { get; set; }
    
    [JsonPropertyName("description")]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
    
    [JsonPropertyName("price")]
    [Range(5000, double.MaxValue, ErrorMessage = "Price must be at least 5,000 VND (VNPay minimum requirement)")]
    public decimal? Price { get; set; }
    
    [JsonPropertyName("duration")]
    [Range(1, 365, ErrorMessage = "Duration must be between 1 and 365 days")]
    public int? Duration { get; set; }
    
    [JsonPropertyName("features")]
    public List<string>? Features { get; set; }
    
    [JsonPropertyName("is_popular")]
    public bool? IsPopular { get; set; }
    
    [JsonPropertyName("display_order")]
    [Range(0, int.MaxValue, ErrorMessage = "Display order must be non-negative")]
    public int? DisplayOrder { get; set; }
    
    [JsonPropertyName("status")]
    public EntityStatus? Status { get; set; }
} 