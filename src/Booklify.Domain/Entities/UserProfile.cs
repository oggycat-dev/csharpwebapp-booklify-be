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
    public string? ReadingPreferences { get; set; }
    
    /// <summary>
    /// User's favorite genres
    /// </summary>
    public string? FavoriteGenres { get; set; }
    
    // /// <summary>
    // /// User's reading level or experience
    // /// </summary>
    // public ReadingLevel ReadingLevel { get; set; } = ReadingLevel.Beginner;
    
    // /// <summary>
    // /// User's favorite books
    // /// </summary>
    // public virtual ICollection<Book> FavoriteBooks { get; set; } = new List<Book>();
    
    // /// <summary>
    // /// User's reading list (books they want to read)
    // /// </summary>
    // public virtual ICollection<Book> ReadingList { get; set; } = new List<Book>();
    
    // /// <summary>
    // /// User's borrowed books
    // /// </summary>
    // public virtual ICollection<BorrowedBook> BorrowedBooks { get; set; } = new List<BorrowedBook>();
    
    // /// <summary>
    // /// User's book reviews
    // /// </summary>
    // public virtual ICollection<BookReview> BookReviews { get; set; } = new List<BookReview>();
    
    // /// <summary>
    // /// User's reading history statistics
    // /// </summary>
    // public virtual ReadingStats? ReadingStats { get; set; }
} 