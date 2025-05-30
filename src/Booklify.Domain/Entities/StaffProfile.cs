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
    /// Staff member's position
    /// </summary>
    public string Position { get; set; } = string.Empty;
    
    /// <summary>
    /// Date when staff member joined
    /// </summary>
    public DateTime JoinDate { get; set; }
    
    /// <summary>
    /// Staff member's work schedule
    /// </summary>
    public string? WorkSchedule { get; set; }
    
    /// <summary>
    /// Staff member's specialization or expertise
    /// </summary>
    public string? Specialization { get; set; }
    
    // /// <summary>
    // /// Books processed by this staff member
    // /// </summary>
    // public virtual ICollection<Book> ProcessedBooks { get; set; } = new List<Book>();
    
    // /// <summary>
    // /// Borrowing records managed by this staff member
    // /// </summary>
    // public virtual ICollection<BorrowingRecord> ManagedBorrowings { get; set; } = new List<BorrowingRecord>();
    
    // /// <summary>
    // /// Book categories managed by this staff member
    // /// </summary>
    // public virtual ICollection<BookCategory> ManagedCategories { get; set; } = new List<BookCategory>();
    
    // /// <summary>
    // /// Performance metrics for the staff member
    // /// </summary>
    // public virtual StaffPerformance? Performance { get; set; }
} 