namespace Booklify.Domain.Enums;

/// <summary>
/// Content approval status for business workflow
/// Controls content quality and approval process
/// </summary>
public enum ApprovalStatus
{
    /// <summary>
    /// Content is waiting for admin review
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Content has been approved and ready for users
    /// </summary>
    Approved = 1,
    
    /// <summary>
    /// Content has been rejected due to quality issues
    /// Staff can edit and resubmit
    /// </summary>
    Rejected = 2
}