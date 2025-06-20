namespace Booklify.Domain.Enums;

/// <summary>
/// Payment status enumeration
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment is pending
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Payment was successful
    /// </summary>
    Success = 1,
    
    /// <summary>
    /// Payment failed
    /// </summary>
    Failed = 2,
    
    /// <summary>
    /// Payment was cancelled
    /// </summary>
    Cancelled = 3,
    
    /// <summary>
    /// Payment was refunded
    /// </summary>
    Refunded = 4,
    
    /// <summary>
    /// Payment is being processed
    /// </summary>
    Processing = 5
} 