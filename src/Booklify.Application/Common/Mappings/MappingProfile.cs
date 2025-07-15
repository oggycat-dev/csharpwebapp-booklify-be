using AutoMapper;
using Booklify.Application.Common.DTOs.Auth;
using Booklify.Domain.Entities.Identity;
using Booklify.Domain.Entities;
using Booklify.Application.Common.DTOs.Staff;
using Booklify.Application.Common.DTOs.BookCategory;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.DTOs.Payment;
using Booklify.Domain.Enums;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.DTOs.ChapterNote;
using Booklify.Application.Common.DTOs.ReadingProgress;

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

        // Book mapping for create request (metadata will be set by EPUB extraction, except ISBN)
        CreateMap<CreateBookRequest, Book>()
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId))
            .ForMember(dest => dest.IsPremium, opt => opt.MapFrom(src => src.IsPremium))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags))
            .ForMember(dest => dest.ISBN, opt => opt.MapFrom(src => src.Isbn ?? string.Empty)) // Allow manual ISBN entry
            .ForMember(dest => dest.ApprovalStatus, opt => opt.MapFrom(src => Domain.Enums.ApprovalStatus.Pending))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Domain.Enums.EntityStatus.Active))
            // Metadata fields will be set by EPUB extraction
            .ForMember(dest => dest.Title, opt => opt.Ignore()) // Set by EPUB extraction
            .ForMember(dest => dest.Description, opt => opt.Ignore()) // Set by EPUB extraction
            .ForMember(dest => dest.Author, opt => opt.Ignore()) // Set by EPUB extraction
            .ForMember(dest => dest.Publisher, opt => opt.Ignore()) // Set by EPUB extraction
            .ForMember(dest => dest.PublishedDate, opt => opt.Ignore()) // Set by EPUB extraction
            .ForMember(dest => dest.PageCount, opt => opt.Ignore()) // Set by EPUB extraction
            .ForMember(dest => dest.CoverImageUrl, opt => opt.Ignore()) // Set by EPUB extraction
            // Ignore properties that will be set elsewhere
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.FilePath, opt => opt.Ignore())
            .ForMember(dest => dest.File, opt => opt.Ignore())
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
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty))
            .ForMember(dest => dest.ApprovalStatus, opt => opt.MapFrom(src => src.ApprovalStatus))
            .ForMember(dest => dest.ApprovalStatusString, opt => opt.MapFrom(src => src.ApprovalStatus.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.StatusString, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => src.CoverImageUrl))
            .ForMember(dest => dest.IsPremium, opt => opt.MapFrom(src => src.IsPremium))
            .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src => src.AverageRating))
            .ForMember(dest => dest.TotalRatings, opt => opt.MapFrom(src => src.TotalRatings))
            .ForMember(dest => dest.TotalViews, opt => opt.MapFrom(src => src.TotalViews))
            .ForMember(dest => dest.TotalPages, opt => opt.MapFrom(src => src.PageCount))
            .ForMember(dest => dest.TotalChapters, opt => opt.MapFrom(src => src.TotalChapters))
            .ForMember(dest => dest.PublishedDate, opt => opt.MapFrom(src => src.PublishedDate))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

        // Map Book to BookResponse
        CreateMap<Domain.Entities.Book, BookResponse>()
            .IncludeBase<Domain.Entities.Book, BookListItemResponse>()
            .ForMember(dest => dest.ISBN, opt => opt.MapFrom(src => src.ISBN))
            .ForMember(dest => dest.Publisher, opt => opt.MapFrom(src => src.Publisher))
            .ForMember(dest => dest.ApprovalNote, opt => opt.MapFrom(src => src.ApprovalNote))
            // Note: Status and StatusString are inherited from BookListItemResponse
            .ForMember(dest => dest.FilePath, opt => opt.MapFrom(src => src.FilePath))
            .ForMember(dest => dest.PageCount, opt => opt.MapFrom(src => src.PageCount))
            .ForMember(dest => dest.ModifiedAt, opt => opt.MapFrom(src => src.ModifiedAt))
            .ForMember(dest => dest.HasChapters, opt => opt.MapFrom(src => src.Chapters != null && src.Chapters.Any()))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags))
            // FileUrl will be set in business logic
            .ForMember(dest => dest.FileUrl, opt => opt.Ignore());

        // Map Book to BookDetailResponse (same as BookResponse but different type)
        CreateMap<Domain.Entities.Book, BookDetailResponse>()
            .IncludeBase<Domain.Entities.Book, BookListItemResponse>()
            .ForMember(dest => dest.ISBN, opt => opt.MapFrom(src => src.ISBN))
            .ForMember(dest => dest.Publisher, opt => opt.MapFrom(src => src.Publisher))
            .ForMember(dest => dest.ApprovalNote, opt => opt.MapFrom(src => src.ApprovalNote))
            // Note: Status and StatusString are inherited from BookListItemResponse
            .ForMember(dest => dest.FilePath, opt => opt.MapFrom(src => src.FilePath))
            .ForMember(dest => dest.PageCount, opt => opt.MapFrom(src => src.PageCount))
            .ForMember(dest => dest.ModifiedAt, opt => opt.MapFrom(src => src.ModifiedAt))
            .ForMember(dest => dest.HasChapters, opt => opt.MapFrom(src => src.Chapters != null && src.Chapters.Any()))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags))
            // FileUrl will be set in business logic
            .ForMember(dest => dest.FileUrl, opt => opt.Ignore());

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
        
        // Subscription history mappings
        CreateMap<UserSubscription, UserSubscriptionHistoryResponse>()
            .ForMember(dest => dest.SubscriptionName, opt => opt.MapFrom(src => src.Subscription.Name))
            .ForMember(dest => dest.SubscriptionDescription, opt => opt.MapFrom(src => src.Subscription.Description))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Subscription.Price))
            .ForMember(dest => dest.DurationDays, opt => opt.MapFrom(src => src.Subscription.Duration))
            .ForMember(dest => dest.StatusString, opt => opt.MapFrom(src => src.Status.ToString()));
        
        // Payment mappings
        CreateMap<Domain.Entities.Payment, PaymentStatusResponse>()
            .ForMember(dest => dest.PaymentId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.SubscriptionActivated, opt => opt.Ignore());
        
        // Payment history mappings
        CreateMap<Domain.Entities.Payment, PaymentHistoryResponse>()
            .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.PaymentStatus.ToString()))
            .ForMember(dest => dest.SubscriptionName, opt => opt.MapFrom(src => 
                src.UserSubscription != null && src.UserSubscription.Subscription != null 
                    ? src.UserSubscription.Subscription.Name 
                    : (string?)null))
            .ForMember(dest => dest.SubscriptionDuration, opt => opt.MapFrom(src => 
                src.UserSubscription != null && src.UserSubscription.Subscription != null 
                    ? (int?)src.UserSubscription.Subscription.Duration 
                    : (int?)null));

        // ChapterNote mappings
        CreateMap<ChapterNote, ChapterNoteListItemResponse>()
            // Map from BaseEntity
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            // Map from ChapterNote
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
            .ForMember(dest => dest.PageNumber, opt => opt.MapFrom(src => src.PageNumber))
            .ForMember(dest => dest.NoteType, opt => opt.MapFrom(src => src.NoteType))
            .ForMember(dest => dest.NoteTypeName, opt => opt.MapFrom(src => src.NoteType.ToString()))
            .ForMember(dest => dest.Cfi, opt => opt.MapFrom(src => src.Cfi))
            .ForMember(dest => dest.HighlightedText, opt => opt.MapFrom(src => src.HighlightedText));
        
        CreateMap<ChapterNote, ChapterNoteResponse>()
            .IncludeBase<ChapterNote, ChapterNoteListItemResponse>()
            // Map nullable fields with null condition
            .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color))
            // Map from navigation properties with null checks
            .ForMember(dest => dest.ChapterTitle, opt => opt.MapFrom(src => 
                src.Chapter != null ? src.Chapter.Title : string.Empty))
            .ForMember(dest => dest.ChapterOrder, opt => opt.MapFrom(src => 
                src.Chapter != null ? src.Chapter.Order : 0))
            .ForMember(dest => dest.BookId, opt => opt.MapFrom(src => 
                src.Chapter != null ? src.Chapter.BookId : Guid.Empty))
            .ForMember(dest => dest.BookTitle, opt => opt.MapFrom(src => 
                src.Chapter != null && src.Chapter.Book != null ? src.Chapter.Book.Title : string.Empty));
        
        CreateMap<CreateChapterNoteRequest, ChapterNote>();
        
        // Chapter mappings
        CreateMap<Chapter, ChapterResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Order, opt => opt.MapFrom(src => src.Order))
            .ForMember(dest => dest.Href, opt => opt.MapFrom(src => src.Href))
            .ForMember(dest => dest.Cfi, opt => opt.MapFrom(src => src.Cfi))
            .ForMember(dest => dest.ParentChapterId, opt => opt.MapFrom(src => src.ParentChapterId))
            .ForMember(dest => dest.ChildChapters, opt => opt.Ignore()); // Will be populated manually

        // ReadingProgress mappings
        CreateMap<TrackingReadingSessionRequest, ReadingProgress>()
            .ForMember(dest => dest.BookId, opt => opt.MapFrom(src => src.BookId))
            .ForMember(dest => dest.LastReadAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.FirstReadAt, opt => opt.MapFrom(src => DateTime.UtcNow)) //only for new reading progress
            // Ignore other properties that will be set by business logic
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.CompletedChaptersCount, opt => opt.Ignore())
            .ForMember(dest => dest.Book, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentChapter, opt => opt.Ignore())
            .ForMember(dest => dest.ChapterProgresses, opt => opt.Ignore())
            .ForMember(dest => dest.IsCompleted, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentChapterId, opt => opt.Ignore());

        // TrackingReadingSessionRequest to ChapterReadingProgress mapping
        CreateMap<TrackingReadingSessionRequest, ChapterReadingProgress>()
            .ForMember(dest => dest.ChapterId, opt => opt.MapFrom(src => src.ChapterId))
            .ForMember(dest => dest.CurrentCfi, opt => opt.MapFrom(src => src.CurrentCfi))
            .ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(src => src.IsCompleted))
            .ForMember(dest => dest.CompletedAt, opt => opt.MapFrom(src => src.IsCompleted ? DateTime.UtcNow : (DateTime?)null))
            .ForMember(dest => dest.LastReadAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            // Ignore other properties that will be set by business logic
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ReadingProgressId, opt => opt.Ignore())
            .ForMember(dest => dest.ReadingProgress, opt => opt.Ignore())
            .ForMember(dest => dest.Chapter, opt => opt.Ignore());

        // Simplified TrackingSessionResponse mapping - no navigation properties needed
        CreateMap<ReadingProgress, TrackingSessionResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.BookId, opt => opt.MapFrom(src => src.BookId))
            .ForMember(dest => dest.CurrentChapterId, opt => opt.MapFrom(src => src.CurrentChapterId))
            .ForMember(dest => dest.CompletedChaptersCount, opt => opt.MapFrom(src => src.CompletedChaptersCount))
            .ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(src => src.IsCompleted))
            .ForMember(dest => dest.LastReadAt, opt => opt.MapFrom(src => src.LastReadAt))
            // TotalChaptersCount and OverallProgressPercentage will be set manually in handler
            .ForMember(dest => dest.TotalChaptersCount, opt => opt.Ignore())
            .ForMember(dest => dest.OverallProgressPercentage, opt => opt.Ignore());

        CreateMap<ReadingProgress, ReadingProgressResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.BookId, opt => opt.MapFrom(src => src.BookId))
            .ForMember(dest => dest.BookTitle, opt => opt.MapFrom(src => src.Book != null ? src.Book.Title : string.Empty))
            .ForMember(dest => dest.CurrentChapterId, opt => opt.MapFrom(src => src.CurrentChapterId))
            .ForMember(dest => dest.CurrentChapterTitle, opt => opt.MapFrom(src => src.CurrentChapter != null ? src.CurrentChapter.Title : null))
            .ForMember(dest => dest.LastReadAt, opt => opt.MapFrom(src => src.LastReadAt))
            .ForMember(dest => dest.FirstReadAt, opt => opt.MapFrom(src => src.FirstReadAt))
            .ForMember(dest => dest.CompletedChaptersCount, opt => opt.MapFrom(src => src.CompletedChaptersCount))
            .ForMember(dest => dest.OverallProgressPercentage, opt => opt.MapFrom(src => 
                src.Book != null && src.Book.TotalChapters > 0 ? 
                Math.Round((double)src.CompletedChaptersCount / src.Book.TotalChapters * 100, 2) : 0))
            // Manual calculation fields - will be set by business logic
            //.ForMember(dest => dest.OverallProgressPercentage, opt => opt.Ignore())
            .ForMember(dest => dest.TotalChaptersCount, opt => opt.MapFrom(src => src.Book != null ? src.Book.TotalChapters : 0))
            .ForMember(dest => dest.CompletedChapterIds, opt => opt.Ignore())
            .ForMember(dest => dest.AccessedChapterIds, opt => opt.Ignore())
            .ForMember(dest => dest.ChapterProgresses, opt => opt.Ignore());

        // ChapterReadingProgress mappings
        CreateMap<ChapterReadingProgress, ChapterReadingProgressResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ReadingProgressId, opt => opt.MapFrom(src => src.ReadingProgressId))
            .ForMember(dest => dest.ChapterId, opt => opt.MapFrom(src => src.ChapterId))
            .ForMember(dest => dest.ChapterTitle, opt => opt.MapFrom(src => src.Chapter != null ? src.Chapter.Title : string.Empty))
            .ForMember(dest => dest.ChapterOrder, opt => opt.MapFrom(src => src.Chapter != null ? src.Chapter.Order : 0))
            .ForMember(dest => dest.CurrentCfi, opt => opt.MapFrom(src => src.CurrentCfi))
            .ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(src => src.IsCompleted))
            .ForMember(dest => dest.CompletedAt, opt => opt.MapFrom(src => src.CompletedAt))
            .ForMember(dest => dest.LastReadAt, opt => opt.MapFrom(src => src.LastReadAt));

        // User mappings
        CreateMap<UserProfile, Booklify.Application.Common.DTOs.User.UserResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
            .ForMember(dest => dest.GenderName, opt => opt.MapFrom(src => GetGenderName(src.Gender)))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.StatusString, opt => opt.MapFrom(src => src.Status.ToString()))
            // Username, Email, IsActive, and HasActiveSubscription will be set manually from IdentityUser and UserSubscriptions
            .ForMember(dest => dest.Username, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.HasActiveSubscription, opt => opt.Ignore());

        CreateMap<UserProfile, Booklify.Application.Common.DTOs.User.UserDetailResponse>()
            .IncludeBase<UserProfile, Booklify.Application.Common.DTOs.User.UserResponse>()
            .ForMember(dest => dest.Birthday, opt => opt.MapFrom(src => src.Birthday))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
            // AvatarUrl, Subscription, HasActiveSubscription will be set manually in handler
            .ForMember(dest => dest.AvatarUrl, opt => opt.Ignore())
            .ForMember(dest => dest.Subscription, opt => opt.Ignore())
            .ForMember(dest => dest.HasActiveSubscription, opt => opt.Ignore());
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
            StaffPosition.LibraryManager => "Library Manager",
            _ => "Unknown"
        };
    }

    // Helper method to get gender name from Gender enum
    private static string? GetGenderName(Gender? gender)
    {
        return gender switch
        {
            Gender.Male => "Male",
            Gender.Female => "Female", 
            Gender.Other => "Other",
            _ => null
        };
    }
}