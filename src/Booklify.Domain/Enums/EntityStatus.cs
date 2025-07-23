namespace Booklify.Domain.Enums;

/// <summary>
/// System-level status for technical operations
/// Controls visibility and accessibility in the system
/// </summary>
public enum EntityStatus
{
    /// <summary>
    /// Entity is temporarily disabled/hidden (maintenance, etc.)
    /// </summary>
    Inactive = 0,
    
    /// <summary>
    /// Entity is active and available in the system
    /// </summary>
    Active = 1,
    
    /// <summary>
    /// Entity is pending (waiting for payment, approval, etc.)
    /// </summary>
    Pending = 2,
    
    /// <summary>
    /// Entity has expired (subscription expired, etc.)
    /// </summary>
    Expired = 3
}