using Booklify.Domain.Commons;
using Booklify.Domain.Enums;

namespace Booklify.Domain.Entities;

/// <summary>
/// User subscription information linking users to their subscription plans
/// </summary>
public class UserSubscription : BaseEntity
{
    /// <summary>
    /// Reference to the user who subscribed
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Navigation property to UserProfile
    /// </summary>
    public virtual UserProfile User { get; set; }
    
    /// <summary>
    /// Reference to the subscription plan
    /// </summary>
    public Guid SubscriptionId { get; set; }
    
    /// <summary>
    /// Navigation property to Subscription
    /// </summary>
    public virtual Subscription Subscription { get; set; }
    
    /// <summary>
    /// When the subscription started
    /// </summary>
    public DateTime StartDate { get; set; }
    
    /// <summary>
    /// When the subscription expires
    /// </summary>
    public DateTime EndDate { get; set; }
    
    /// <summary>
    /// Whether the subscription is currently active
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Auto-renewal setting
    /// </summary>
    public bool AutoRenew { get; set; }
    
    /// <summary>
    /// Subscription status
    /// </summary>
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    
    /// <summary>
    /// Navigation property to payments for this subscription
    /// </summary>
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
} 