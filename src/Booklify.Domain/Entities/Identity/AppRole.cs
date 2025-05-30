using Microsoft.AspNetCore.Identity;

namespace Booklify.Domain.Entities.Identity;

/// <summary>
/// Application role with additional properties
/// </summary>
public class AppRole : IdentityRole
{
    /// <summary>
    /// Role description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime? ModifiedAt { get; set; }
    
    /// <summary>
    /// Whether the role is a system role (cannot be deleted)
    /// </summary>
    public bool IsSystemRole { get; set; } = false;
} 