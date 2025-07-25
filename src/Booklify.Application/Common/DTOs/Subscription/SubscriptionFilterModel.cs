using System.Text.Json.Serialization;
using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;

namespace Booklify.Application.Common.DTOs.Subscription;

/// <summary>
/// Filter model for subscription plans
/// </summary>
public class SubscriptionFilterModel : FilterBase
{
    
    public string? Name { get; set; }
    
    public decimal? MinPrice { get; set; }
    
    public decimal? MaxPrice { get; set; }
    
    public int? MinDuration { get; set; }
    
    public int? MaxDuration { get; set; }
    
    public EntityStatus? Status { get; set; }
    
    public bool? IsPopular { get; set; }
} 