using Booklify.Domain.Commons;
using Booklify.Domain.Entities.Identity;
using Booklify.Domain.Enums;

namespace Booklify.Domain.Entities;

/// <summary>
/// Staff member profile with basic properties
/// </summary>
public class StaffProfile : BaseEntity
{
    /// <summary>
    /// First name of the person
    /// </summary>
    public string? StaffCode { get; set; }
    public string FirstName { get; set; } = string.Empty;
    
    /// <summary>
    /// Last name of the person
    /// </summary>
    public string LastName { get; set; } = string.Empty;
    
    /// <summary>
    /// Full name of the person
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// Person's date of birth
    /// </summary>
    public DateTime? Birthday { get; set; }
    
    /// <summary>
    /// Person's gender
    /// </summary>
    public Gender? Gender { get; set; }
    
    /// <summary>
    /// Person's phone number
    /// </summary>
    public string? Phone { get; set; }

    public string? Email { get; set; }
    
    /// <summary>
    /// Person's address
    /// </summary>
    public string? Address { get; set; }
    
    /// <summary>
    /// Person's profile picture URL
    /// </summary>
    public string? ProfilePictureUrl { get; set; }
    
    /// <summary>
    /// Reference to ASP.NET Identity User
    /// </summary>
    public string? IdentityUserId { get; set; }
    
    /// <summary>
    /// Navigation property to Identity User
    /// </summary>
    public virtual AppUser? IdentityUser { get; set; }

    /// <summary>
    /// Date when staff member joined
    /// </summary>
    public DateTime JoinDate { get; set; }
    public DateTime? LeaveDate { get; set; }
    public string? LeaveNote { get; set; }
    public StaffPosition Position { get; set; } = StaffPosition.Unknown;
    
    
    /// <summary>
    /// Staff member's avatar
    /// </summary>
    public virtual FileInfo? Avatar { get; set; }
    
    /// <summary>
    /// Staff member's avatar ID
    /// </summary>
    public Guid? AvatarId { get; set; }

    public EntityStatus Status { get; set; } = EntityStatus.Active;
} 