using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Booklify.Domain.Entities;
using Booklify.Domain.Entities.Identity;
using Booklify.Domain.Commons;
using System.Linq.Expressions;
using Booklify.Application.Common.Interfaces;
using FileInfo = Booklify.Domain.Entities.FileInfo;

namespace Booklify.Infrastructure.Persistence;

public class BooklifyDbContext : DbContext, IBooklifyDbContext
{
    public BooklifyDbContext(DbContextOptions<BooklifyDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(CoreEventId.FirstWithoutOrderByAndFilterWarning));
    }

    // User and staff profiles
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<StaffProfile> StaffProfiles { get; set; }
    
    // File information
    public DbSet<FileInfo> FileInfos { get; set; }
    
    // Book and Category
    public DbSet<Book> Books { get; set; }
    public DbSet<BookCategory> BookCategories { get; set; }
    public DbSet<Chapter> Chapters { get; set; }
    public DbSet<ChapterNote> ChapterNotes { get; set; }
    public DbSet<ReadingProgress> ReadingProgresses { get; set; }
    
    // AI-related entities
    public DbSet<ChapterAIResult> ChapterAIResults { get; set; }
    
    // Subscription and Payment
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<UserSubscription> UserSubscriptions { get; set; }
    public DbSet<Payment> Payments { get; set; }

    // Seeding status tracking
    public DbSet<SeedTracking> SeedTrackings { get; set; }

    // Identity references (navigation only, not for migrations)
    public DbSet<AppUser> IdentityUsers { get; set; }
    public DbSet<AppRole> IdentityRoles { get; set; }

    /// <summary>
    /// Override SaveChangesAsync to prevent AppUser entities from being saved by Booklify context
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Detach any AppUser or AppRole entities to prevent FK violations
        // Since they are managed by Identity context, not Booklify context
        var identityEntries = ChangeTracker.Entries()
            .Where(e => e.Entity is AppUser || e.Entity is AppRole)
            .ToList();

        foreach (var entry in identityEntries)
        {
            entry.State = EntityState.Detached;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Override SaveChanges to prevent AppUser entities from being saved by Booklify context
    /// </summary>
    public override int SaveChanges()
    {
        // Detach any AppUser or AppRole entities to prevent FK violations
        var identityEntries = ChangeTracker.Entries()
            .Where(e => e.Entity is AppUser || e.Entity is AppRole)
            .ToList();

        foreach (var entry in identityEntries)
        {
            entry.State = EntityState.Detached;
        }

        return base.SaveChanges();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply soft delete filter to all entities inheriting from BaseEntity
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var falseConstant = Expression.Constant(false);
                var condition = Expression.Equal(property, falseConstant);
                var lambda = Expression.Lambda(condition, parameter);
                
                builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }

        // Configure FileInfo
        builder.Entity<FileInfo>(entity =>
        {
            entity.ToTable("FileInfos");
            
            // Required fields
            entity.Property(f => f.FilePath).IsRequired();
            entity.Property(f => f.ServerUpload).IsRequired();
            entity.Property(f => f.Provider).IsRequired();
            entity.Property(f => f.Name).IsRequired();
            entity.Property(f => f.MimeType).IsRequired();
            entity.Property(f => f.Extension).IsRequired();
            
            // Add index for common queries
            entity.HasIndex(f => f.FilePath);
            entity.HasIndex(f => f.Provider);
        });

        // Configure UserProfile
        builder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("UserProfiles");
            
            // Configure relationship with AppUser
            entity.HasOne(u => u.IdentityUser)
                .WithOne(i => i.UserProfile)
                .HasForeignKey<UserProfile>(u => u.IdentityUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure relationship with Avatar (FileInfo)
            entity.HasOne(u => u.Avatar)
                .WithOne()
                .HasForeignKey<UserProfile>(u => u.AvatarId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(u => u.Gender)
                .HasConversion<int>();
                
            // Add index for IdentityUserId
            entity.HasIndex(u => u.IdentityUserId)
                .IsUnique()
                .HasFilter("[IdentityUserId] IS NOT NULL");
                
            // Add index for AvatarId
            entity.HasIndex(u => u.AvatarId)
                .IsUnique()
                .HasFilter("[AvatarId] IS NOT NULL");
        });
        
        // Configure StaffProfile
        builder.Entity<StaffProfile>(entity =>
        {
            entity.ToTable("StaffProfiles");
            
            // Configure relationship with AppUser
            entity.HasOne(s => s.IdentityUser)
                .WithOne(i => i.StaffProfile)
                .HasForeignKey<StaffProfile>(s => s.IdentityUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure relationship with Avatar (FileInfo)
            entity.HasOne(s => s.Avatar)
                .WithOne()
                .HasForeignKey<StaffProfile>(s => s.AvatarId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(s => s.Gender)
                .HasConversion<int>();
                
            // Add index for IdentityUserId
            entity.HasIndex(s => s.IdentityUserId)
                .IsUnique()
                .HasFilter("[IdentityUserId] IS NOT NULL");
                
            // Add index for AvatarId
            entity.HasIndex(s => s.AvatarId)
                .IsUnique()
                .HasFilter("[AvatarId] IS NOT NULL");
        });

        // Configure BookCategory
        builder.Entity<BookCategory>(entity =>
        {
            entity.ToTable("BookCategories");
            
            // Required fields
            entity.Property(c => c.Name).IsRequired();
            entity.Property(c => c.Description).IsRequired(false);
            
            // Convert Status enum to int
            entity.Property(c => c.Status)
                .HasConversion<int>();
            
            // Add index for Name (unique)
            entity.HasIndex(c => c.Name)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
                
            // Add index for Status
            entity.HasIndex(c => c.Status);
        });

        // Configure Book
        builder.Entity<Book>(entity =>
        {
            entity.ToTable("Books");
            
            // Required fields
            entity.Property(b => b.Title).IsRequired(false);
            entity.Property(b => b.Description).IsRequired(false);
            entity.Property(b => b.Author).IsRequired(false);
            entity.Property(b => b.ISBN).IsRequired(false);
            entity.Property(b => b.Publisher).IsRequired(false);
            entity.Property(b => b.Tags).IsRequired(false);
            entity.Property(b => b.CoverImageUrl).IsRequired(false);
            entity.Property(b => b.FilePath).IsRequired(false);
            
            // Convert enums to int
            entity.Property(b => b.ApprovalStatus)
                .HasConversion<int>();
            entity.Property(b => b.Status)
                .HasConversion<int>();
            
            // Configure relationship with BookCategory
            entity.HasOne(b => b.Category)
                .WithMany(c => c.Books)
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Configure relationship with FileInfo
            entity.HasOne(b => b.File)
                .WithOne()
                .HasForeignKey<Book>(b => b.FilePath)
                .HasPrincipalKey<FileInfo>(f => f.FilePath)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
            
            // Configure relationship with Chapters (CASCADE DELETE)
            entity.HasMany(b => b.Chapters)
                .WithOne(c => c.Book)
                .HasForeignKey(c => c.BookId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Add indexes for performance
            entity.HasIndex(b => b.Title);
            entity.HasIndex(b => b.Author);
            entity.HasIndex(b => b.CategoryId);
            entity.HasIndex(b => b.ApprovalStatus);
            entity.HasIndex(b => b.Status);
            entity.HasIndex(b => b.IsPremium);
            entity.HasIndex(b => b.ISBN);
            
            // Composite indexes for common queries
            entity.HasIndex(b => new { b.ApprovalStatus, b.Status, b.IsPremium });
            entity.HasIndex(b => new { b.CategoryId, b.ApprovalStatus, b.Status });
        });

        // Configure Chapter
        builder.Entity<Chapter>(entity =>
        {
            entity.ToTable("Chapters");
            
            // Required fields
            entity.Property(c => c.Title).IsRequired();
            entity.Property(c => c.Order).IsRequired();
            entity.Property(c => c.Href).IsRequired(false);
            entity.Property(c => c.Cfi).IsRequired(false);
            
            // Convert Status enum to int
            entity.Property(c => c.Status)
                .HasConversion<int>();
            
            // Configure self-referencing relationship for parent-child chapters
            entity.HasOne(c => c.ParentChapter)
                .WithMany(c => c.ChildChapters)
                .HasForeignKey(c => c.ParentChapterId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete loops
            
            // Configure relationship with Book (already configured above but adding for completeness)
            entity.HasOne(c => c.Book)
                .WithMany(b => b.Chapters)
                .HasForeignKey(c => c.BookId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Add indexes for performance
            entity.HasIndex(c => c.BookId);
            entity.HasIndex(c => c.ParentChapterId);
            entity.HasIndex(c => c.Order);
            entity.HasIndex(c => c.Status);
            
            // Composite indexes for common queries
            entity.HasIndex(c => new { c.BookId, c.Order });
            entity.HasIndex(c => new { c.BookId, c.Status });
            entity.HasIndex(c => new { c.ParentChapterId, c.Order });
        });

        // Configure Subscription
        builder.Entity<Subscription>(entity =>
        {
            entity.ToTable("Subscriptions");
            
            // Required fields
            entity.Property(s => s.Name).IsRequired();
            entity.Property(s => s.Description).IsRequired();
            entity.Property(s => s.Price).HasPrecision(18, 2);
            
            // Convert Status enum to int
            entity.Property(s => s.Status)
                .HasConversion<int>();
            
            // Add index for Name (unique)
            entity.HasIndex(s => s.Name)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
                
            // Add index for Status
            entity.HasIndex(s => s.Status);
        });

        // Configure UserSubscription
        builder.Entity<UserSubscription>(entity =>
        {
            entity.ToTable("UserSubscriptions");
            
            // Configure relationship with UserProfile
            entity.HasOne(us => us.User)
                .WithMany(u => u.UserSubscriptions)
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Configure relationship with Subscription
            entity.HasOne(us => us.Subscription)
                .WithMany(s => s.UserSubscriptions)
                .HasForeignKey(us => us.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Convert Status enum to int
            entity.Property(us => us.Status)
                .HasConversion<int>();
            
            // Add indexes for performance
            entity.HasIndex(us => us.UserId);
            entity.HasIndex(us => us.SubscriptionId);
            entity.HasIndex(us => us.IsActive);
            entity.HasIndex(us => new { us.StartDate, us.EndDate });
            
            // Composite index for common queries
            entity.HasIndex(us => new { us.UserId, us.IsActive, us.Status });
        });

        // Configure Payment
        builder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");
            
            // Configure relationship with UserSubscription
            entity.HasOne(p => p.UserSubscription)
                .WithMany(us => us.Payments)
                .HasForeignKey(p => p.UserSubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Required fields
            entity.Property(p => p.PaymentMethod).IsRequired();
            entity.Property(p => p.Currency).IsRequired();
            entity.Property(p => p.Amount).HasPrecision(18, 2);
            
            // Convert PaymentStatus enum to int
            entity.Property(p => p.PaymentStatus)
                .HasConversion<int>();
            
            // Add indexes for performance
            entity.HasIndex(p => p.UserSubscriptionId);
            entity.HasIndex(p => p.PaymentStatus);
            entity.HasIndex(p => p.TransactionId);
            entity.HasIndex(p => p.PaymentDate);
            
            // Composite index for common queries
            entity.HasIndex(p => new { p.PaymentStatus, p.PaymentDate });
        });

        // Configure ChapterAIResult
        builder.Entity<ChapterAIResult>(entity =>
        {
            entity.ToTable("ChapterAIResults");
            
            // Configure properties
            entity.Property(c => c.Summary).IsRequired(false);
            entity.Property(c => c.Translation).IsRequired(false);
            entity.Property(c => c.Keywords).IsRequired(false);
            entity.Property(c => c.Flashcards).IsRequired(false);
            entity.Property(c => c.AIModel).IsRequired(false).HasMaxLength(50);
            entity.Property(c => c.ProcessedActions).IsRequired().HasMaxLength(200);
            
            // Convert Status enum to int
            entity.Property(c => c.Status)
                .HasConversion<int>();
            
            // Configure relationship with Chapter
            entity.HasOne(c => c.Chapter)
                .WithOne()
                .HasForeignKey<ChapterAIResult>(c => c.ChapterId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Add indexes
            entity.HasIndex(c => c.ChapterId);
            entity.HasIndex(c => c.Status);
        });

        // Configure ChapterNote
        builder.Entity<ChapterNote>(entity =>
        {
            entity.ToTable("ChapterNotes");
            
            // Required fields
            entity.Property(n => n.Content).IsRequired();
            entity.Property(n => n.PageNumber).IsRequired();
            entity.Property(n => n.Cfi).IsRequired(false);
            entity.Property(n => n.HighlightedText).IsRequired(false);
            entity.Property(n => n.Color).IsRequired(false);
            
            // Convert Status enum to int
            entity.Property(n => n.Status)
                .HasConversion<int>();
            
            // Configure relationships
            entity.HasOne(n => n.Chapter)
                .WithMany(c => c.Notes)
                .HasForeignKey(n => n.ChapterId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(n => n.User)
                .WithMany(u => u.ChapterNotes)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Add indexes for performance
            entity.HasIndex(n => n.ChapterId);
            entity.HasIndex(n => n.UserId);
            entity.HasIndex(n => n.Status);
            
            // Composite indexes for common queries
            entity.HasIndex(n => new { n.ChapterId, n.UserId });
            entity.HasIndex(n => new { n.UserId, n.Status });
        });

        // Configure ReadingProgress (EPUB-focused)
        builder.Entity<ReadingProgress>(entity =>
        {
            entity.ToTable("ReadingProgresses");
            
            // Required fields for EPUB tracking
            entity.Property(rp => rp.CurrentCfi).IsRequired().HasMaxLength(500);
            entity.Property(rp => rp.LastReadAt).IsRequired();
            entity.Property(rp => rp.TotalReadingTimeMinutes).IsRequired();
            
            // Progress percentage fields with precision
            entity.Property(rp => rp.ChapterCompletionPercentage).HasPrecision(5, 2); // 0.00 - 100.00
            entity.Property(rp => rp.CfiProgressPercentage).HasPrecision(5, 2); // 0.00 - 100.00
            entity.Property(rp => rp.OverallProgressPercentage).HasPrecision(5, 2); // 0.00 - 100.00
            
            // Optional fields
            entity.Property(rp => rp.CompletedChapterIds).IsRequired(false).HasMaxLength(2000); // JSON string
            entity.Property(rp => rp.SessionStartTime).IsRequired(false);
            
            // Configure relationships with CASCADE delete
            entity.HasOne(rp => rp.Book)
                .WithMany(b => b.ReadingProgresses)
                .HasForeignKey(rp => rp.BookId)
                .OnDelete(DeleteBehavior.Cascade); // When Book is deleted, delete ReadingProgress
                
            entity.HasOne(rp => rp.User)
                .WithMany(u => u.ReadingProgresses)
                .HasForeignKey(rp => rp.UserId)
                .OnDelete(DeleteBehavior.Cascade); // When User is deleted, delete ReadingProgress
                
            entity.HasOne(rp => rp.CurrentChapter)
                .WithMany()
                .HasForeignKey(rp => rp.CurrentChapterId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction); // Avoid cascade path conflicts - Chapter deletion handled by Book cascade
            
            // Add indexes for EPUB-specific queries
            entity.HasIndex(rp => rp.BookId);
            entity.HasIndex(rp => rp.UserId);
            entity.HasIndex(rp => rp.CurrentChapterId);
            entity.HasIndex(rp => rp.LastReadAt);
            entity.HasIndex(rp => rp.CurrentCfi); // For CFI-based queries
            
            // Composite indexes for common EPUB reading patterns
            entity.HasIndex(rp => new { rp.UserId, rp.BookId }); // For finding user's progress on specific book
            entity.HasIndex(rp => new { rp.UserId, rp.LastReadAt }); // For user's recent reading
            entity.HasIndex(rp => new { rp.BookId, rp.LastReadAt }); // For book's recent readers
            entity.HasIndex(rp => new { rp.BookId, rp.OverallProgressPercentage }); // For book completion analytics
            entity.HasIndex(rp => new { rp.UserId, rp.OverallProgressPercentage }); // For user reading completion analytics
        });

        // Identity-related configurations
        builder.Entity<AppUser>().ToTable("Users", t => t.ExcludeFromMigrations());
        builder.Entity<AppRole>().ToTable("Roles", t => t.ExcludeFromMigrations());
    }
}