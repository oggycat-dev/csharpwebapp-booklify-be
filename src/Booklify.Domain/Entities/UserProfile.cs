using Booklify.Domain.Commons;
using Booklify.Domain.Entities.Identity;
using Booklify.Domain.Enums;

namespace Booklify.Domain.Entities;

/// <summary>
/// Regular user profile with basic properties
/// </summary>
public class UserProfile : BaseEntity
{
    /// <summary>
    /// First name of the person
    /// </summary>
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
    /// User's reading preferences or interests
    /// </summary>
    public virtual FileInfo? Avatar { get; set; }
    
    /// <summary>
    /// User's avatar ID
    /// </summary>
    public Guid? AvatarId { get; set; }

    public EntityStatus Status { get; set; } = EntityStatus.Active;
    
    /// <summary>
    /// Navigation property to user subscriptions
    /// </summary>
    public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
} 