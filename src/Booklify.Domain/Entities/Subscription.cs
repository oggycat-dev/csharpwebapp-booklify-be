using Booklify.Domain.Commons;
using Booklify.Domain.Enums;

namespace Booklify.Domain.Entities;

/// <summary>
/// Subscription plan information
/// </summary>
public class Subscription : BaseEntity
{
    /// <summary>
    /// Subscription plan name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Subscription plan description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Subscription price
    /// </summary>
    public decimal Price { get; set; }
    
    /// <summary>
    /// Duration in days
    /// </summary>
    public int Duration { get; set; }
    
    /// <summary>
    /// Features included in this subscription (semicolon-separated)
    /// </summary>
    public string? Features { get; set; }
    
    /// <summary>
    /// Whether this subscription is marked as popular
    /// </summary>
    public bool IsPopular { get; set; }
    
    /// <summary>
    /// Display order for sorting subscription plans
    /// </summary>
    public int DisplayOrder { get; set; }
    
    /// <summary>
    /// Whether this subscription is active
    /// </summary>
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    
    /// <summary>
    /// Navigation property to user subscriptions
    /// </summary>
    public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
}
