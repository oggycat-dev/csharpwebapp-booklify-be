using AutoMapper;
using Booklify.Application.Common.DTOs.Auth;
using Booklify.Domain.Entities.Identity;
using Booklify.Domain.Entities;
using Booklify.Application.Common.DTOs.Staff;
using Booklify.Application.Common.DTOs.BookCategory;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Domain.Enums;
using Booklify.Application.Common.DTOs.Book;

namespace Booklify.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Auth mapping - AppUser to AuthenticationResponse
        CreateMap<AppUser, AuthenticationResponse>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.UserName ?? string.Empty))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? string.Empty))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => GetDisplayName(src)))
            // These fields should be populated manually in handlers
            .ForMember(dest => dest.AppRole, opt => opt.Ignore())
            .ForMember(dest => dest.AccessToken, opt => opt.Ignore())
            .ForMember(dest => dest.TokenExpiresIn, opt => opt.Ignore());

        // User registration mapping
        CreateMap<UserRegistrationRequest, AppUser>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            // Ignore other Identity properties
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.NormalizedUserName, opt => opt.Ignore())
            .ForMember(dest => dest.NormalizedEmail, opt => opt.Ignore())
            .ForMember(dest => dest.EmailConfirmed, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
            .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
            .ForMember(dest => dest.PhoneNumberConfirmed, opt => opt.Ignore())
            .ForMember(dest => dest.TwoFactorEnabled, opt => opt.Ignore())
            .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
            .ForMember(dest => dest.LockoutEnabled, opt => opt.Ignore())
            .ForMember(dest => dest.AccessFailedCount, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
            .ForMember(dest => dest.EntityId, opt => opt.Ignore())
            .ForMember(dest => dest.RefreshToken, opt => opt.Ignore())
            .ForMember(dest => dest.RefreshTokenExpiryTime, opt => opt.Ignore())
            .ForMember(dest => dest.UserProfile, opt => opt.Ignore())
            .ForMember(dest => dest.StaffProfile, opt => opt.Ignore());

        // Staff registration mapping  
        CreateMap<StaffRegistrationRequest, AppUser>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            // Ignore other Identity properties
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.NormalizedUserName, opt => opt.Ignore())
            .ForMember(dest => dest.NormalizedEmail, opt => opt.Ignore())
            .ForMember(dest => dest.EmailConfirmed, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
            .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
            .ForMember(dest => dest.PhoneNumberConfirmed, opt => opt.Ignore())
            .ForMember(dest => dest.TwoFactorEnabled, opt => opt.Ignore())
            .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
            .ForMember(dest => dest.LockoutEnabled, opt => opt.Ignore())
            .ForMember(dest => dest.AccessFailedCount, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
            .ForMember(dest => dest.EntityId, opt => opt.Ignore())
            .ForMember(dest => dest.RefreshToken, opt => opt.Ignore())
            .ForMember(dest => dest.RefreshTokenExpiryTime, opt => opt.Ignore())
            .ForMember(dest => dest.UserProfile, opt => opt.Ignore())
            .ForMember(dest => dest.StaffProfile, opt => opt.Ignore());

        // Staff profile mapping for registration
        CreateMap<StaffRegistrationRequest, StaffProfile>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => GetFirstName(src.FullName)))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => GetLastName(src.FullName)))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.JoinDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Domain.Enums.EntityStatus.Active))
            // Ignore other properties
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Birthday, opt => opt.Ignore())
            .ForMember(dest => dest.Gender, opt => opt.Ignore())
            .ForMember(dest => dest.Address, opt => opt.Ignore())
            .ForMember(dest => dest.ProfilePictureUrl, opt => opt.Ignore())
            .ForMember(dest => dest.IdentityUserId, opt => opt.Ignore())
            .ForMember(dest => dest.IdentityUser, opt => opt.Ignore())
            .ForMember(dest => dest.Avatar, opt => opt.Ignore())
            .ForMember(dest => dest.AvatarId, opt => opt.Ignore());

        // User profile mapping for registration  
        CreateMap<UserRegistrationRequest, UserProfile>()
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.PhoneNumber))
            // Set default empty values for required fields
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => string.Empty))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => string.Empty))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => string.Empty))
            // Ignore other properties
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Birthday, opt => opt.Ignore())
            .ForMember(dest => dest.Gender, opt => opt.Ignore())
            .ForMember(dest => dest.Address, opt => opt.Ignore())
            .ForMember(dest => dest.ProfilePictureUrl, opt => opt.Ignore())
            .ForMember(dest => dest.IdentityUserId, opt => opt.Ignore())
            .ForMember(dest => dest.IdentityUser, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Domain.Enums.EntityStatus.Active))
            .ForMember(dest => dest.Avatar, opt => opt.Ignore())
            .ForMember(dest => dest.AvatarId, opt => opt.Ignore());
            
        // Staff profile mapping for create request
        CreateMap<CreateStaffRequest, StaffProfile>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
            .ForMember(dest => dest.StaffCode, opt => opt.MapFrom(src => src.StaffCode))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
            .ForMember(dest => dest.Position, opt => opt.MapFrom(src => src.Position))
            .ForMember(dest => dest.JoinDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Domain.Enums.EntityStatus.Active))
            // Ignore other properties
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Birthday, opt => opt.Ignore())
            .ForMember(dest => dest.Gender, opt => opt.Ignore())
            .ForMember(dest => dest.ProfilePictureUrl, opt => opt.Ignore())
            .ForMember(dest => dest.IdentityUserId, opt => opt.Ignore())
            .ForMember(dest => dest.IdentityUser, opt => opt.Ignore())
            .ForMember(dest => dest.LeaveDate, opt => opt.Ignore())
            .ForMember(dest => dest.LeaveNote, opt => opt.Ignore())
            .ForMember(dest => dest.Avatar, opt => opt.Ignore())
            .ForMember(dest => dest.AvatarId, opt => opt.Ignore());
            
        // Map StaffProfile to CreatedStaffResponse
        CreateMap<StaffProfile, CreatedStaffResponse>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName ?? string.Empty))
            .ForMember(dest => dest.StaffCode, opt => opt.MapFrom(src => src.StaffCode ?? string.Empty))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone ?? string.Empty))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address ?? string.Empty))
            .ForMember(dest => dest.Position, opt => opt.MapFrom(src => src.Position.ToString()))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? string.Empty));

        // BookCategory mapping for create request
        CreateMap<CreateBookCategoryRequest, BookCategory>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Domain.Enums.EntityStatus.Active))
            // Ignore other properties
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Books, opt => opt.Ignore());
            
        // Map BookCategory to CreatedBookCategoryResponse
        CreateMap<BookCategory, CreatedBookCategoryResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));
            
        // Map BookCategory to BookCategoryResponse  
        CreateMap<BookCategory, BookCategoryResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.BooksCount, opt => opt.MapFrom(src => src.Books != null ? src.Books.Count : 0))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));
            
        // Map StaffProfile to StaffResponse
        CreateMap<StaffProfile, StaffResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName ?? string.Empty))
            .ForMember(dest => dest.StaffCode, opt => opt.MapFrom(src => src.StaffCode ?? string.Empty))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? string.Empty))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone ?? string.Empty))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address ?? string.Empty))
            .ForMember(dest => dest.Position, opt => opt.MapFrom(src => GetPositionName(src.Position)))
            .ForMember(dest => dest.PositionId, opt => opt.MapFrom(src => src.Position))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IdentityUser != null && src.IdentityUser.IsActive))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

        // Book mapping for create request
        CreateMap<CreateBookRequest, Book>()
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Author))
            .ForMember(dest => dest.ISBN, opt => opt.MapFrom(src => src.ISBN))
            .ForMember(dest => dest.Publisher, opt => opt.MapFrom(src => src.Publisher))
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId))
            .ForMember(dest => dest.IsPremium, opt => opt.MapFrom(src => src.IsPremium))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags))
            .ForMember(dest => dest.PublishedDate, opt => opt.MapFrom(src => src.PublishedDate))
            .ForMember(dest => dest.ApprovalStatus, opt => opt.MapFrom(src => Domain.Enums.ApprovalStatus.Pending))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Domain.Enums.EntityStatus.Active))
            // Ignore properties that will be set elsewhere
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.FilePath, opt => opt.Ignore())
            .ForMember(dest => dest.File, opt => opt.Ignore())
            .ForMember(dest => dest.CoverImageUrl, opt => opt.Ignore())
            .ForMember(dest => dest.PageCount, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForMember(dest => dest.Chapters, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovalNote, opt => opt.Ignore())
            .ForMember(dest => dest.AverageRating, opt => opt.Ignore())
            .ForMember(dest => dest.TotalRatings, opt => opt.Ignore());

        // Map Book to BookListItemResponse
        CreateMap<Domain.Entities.Book, BookListItemResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Author))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => src.CoverImageUrl))
            .ForMember(dest => dest.IsPremium, opt => opt.MapFrom(src => src.IsPremium))
            .ForMember(dest => dest.HasChapters, opt => opt.MapFrom(src => src.Chapters != null && src.Chapters.Any()))
            .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src => src.AverageRating))
            .ForMember(dest => dest.TotalRatings, opt => opt.MapFrom(src => src.TotalRatings))
            .ForMember(dest => dest.PublishedDate, opt => opt.MapFrom(src => src.PublishedDate))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags));

        // Map Book to BookResponse
        CreateMap<Domain.Entities.Book, BookResponse>()
            .IncludeBase<Domain.Entities.Book, BookListItemResponse>()
            .ForMember(dest => dest.ISBN, opt => opt.MapFrom(src => src.ISBN))
            .ForMember(dest => dest.Publisher, opt => opt.MapFrom(src => src.Publisher))
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId))
            .ForMember(dest => dest.ApprovalStatus, opt => opt.MapFrom(src => src.ApprovalStatus.ToString()))
            .ForMember(dest => dest.ApprovalNote, opt => opt.MapFrom(src => src.ApprovalNote))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.FilePath, opt => opt.MapFrom(src => src.FilePath))
            .ForMember(dest => dest.PageCount, opt => opt.MapFrom(src => src.PageCount))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.ModifiedAt, opt => opt.MapFrom(src => src.ModifiedAt))
            // URL and Chapters will be set in business logic
            .ForMember(dest => dest.FileUrl, opt => opt.Ignore())
            .ForMember(dest => dest.Chapters, opt => opt.Ignore());

        // Map Chapter to ChapterResponse
        CreateMap<Domain.Entities.Chapter, Booklify.Application.Common.DTOs.Book.ChapterResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Order, opt => opt.MapFrom(src => src.Order))
            .ForMember(dest => dest.Href, opt => opt.MapFrom(src => src.Href))
            .ForMember(dest => dest.Cfi, opt => opt.MapFrom(src => src.Cfi))
            .ForMember(dest => dest.ParentChapterId, opt => opt.MapFrom(src => src.ParentChapterId))
            .ForMember(dest => dest.ChildChapters, opt => opt.Ignore()); // Will be mapped manually for nested structure
        // Subscription mappings
        CreateMap<Domain.Entities.Subscription, SubscriptionResponse>()
            .ForMember(dest => dest.Features, opt => opt.MapFrom(src => 
                string.IsNullOrEmpty(src.Features) ? new List<string>() : src.Features.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()));
        CreateMap<UserSubscription, UserSubscriptionResponse>()
            .ForMember(dest => dest.Subscription, opt => opt.MapFrom(src => src.Subscription));
        
        // Payment mappings
        CreateMap<Domain.Entities.Payment, PaymentStatusResponse>()
            .ForMember(dest => dest.PaymentId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.SubscriptionActivated, opt => opt.Ignore());
    }

    // Helper method to get display name from user profiles
    private static string? GetDisplayName(AppUser user)
    {
        if (user.UserProfile != null && !string.IsNullOrEmpty(user.UserProfile.FullName))
        {
            return user.UserProfile.FullName;
        }
        
        if (user.StaffProfile != null && !string.IsNullOrEmpty(user.StaffProfile.FullName))
        {
            return user.StaffProfile.FullName;
        }
        
        return null;
    }


    // Helper method to extract first name from full name
    private static string GetFirstName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return string.Empty;
            
        var nameParts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return nameParts.Length > 0 ? nameParts[nameParts.Length - 1] : string.Empty;
    }

    // Helper method to extract last name from full name
    private static string GetLastName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return string.Empty;
            
        var nameParts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return nameParts.Length > 1 ? string.Join(" ", nameParts, 0, nameParts.Length - 1) : string.Empty;
    }

    // Helper method to get position name from StaffPosition enum
    private static string GetPositionName(StaffPosition position)
    {
        return position switch
        {
            StaffPosition.Administrator => "Administrator",
            StaffPosition.Staff => "Staff",
            StaffPosition.UserManager => "User Manager",
            StaffPosition.LibraryManager => "Library Manager",
            StaffPosition.TechnicalSupport => "Technical Support",
            StaffPosition.DataEntryClerk => "Data Entry Clerk",
            StaffPosition.CommunityModerator => "Community Moderator",
            StaffPosition.AIAssistantManager => "AI Assistant Manager",
            _ => "Unknown"
        };
    }
} 