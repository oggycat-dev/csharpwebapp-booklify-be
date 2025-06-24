using Booklify.Domain.Commons;
using Booklify.Domain.Enums;

namespace Booklify.Domain.Entities;

/// <summary>
/// Payment information for subscription transactions
/// </summary>
public class Payment : BaseEntity
{
    /// <summary>
    /// Reference to the user subscription
    /// </summary>
    public Guid UserSubscriptionId { get; set; }
    
    /// <summary>
    /// Navigation property to UserSubscription
    /// </summary>
    public virtual UserSubscription UserSubscription { get; set; }
    
    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Payment method (VNPay, Credit Card, etc.)
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;
    
    /// <summary>
    /// Payment status
    /// </summary>
    public PaymentStatus PaymentStatus { get; set; }
    
    /// <summary>
    /// External transaction ID from payment provider
    /// </summary>
    public string? TransactionId { get; set; }
    
    /// <summary>
    /// When the payment was made
    /// </summary>
    public DateTime PaymentDate { get; set; }
    
    /// <summary>
    /// Payment provider response data (JSON)
    /// </summary>
    public string? ProviderResponse { get; set; }
    
    /// <summary>
    /// Additional notes or description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Currency code (VND, USD, etc.)
    /// </summary>
    public string Currency { get; set; } = "VND";
} 