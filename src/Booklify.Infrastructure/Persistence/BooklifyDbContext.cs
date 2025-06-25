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
            
            // Add indexes for performance
            entity.HasIndex(b => b.Title);
            entity.HasIndex(b => b.Author);
            entity.HasIndex(b => b.CategoryId);
            entity.HasIndex(b => b.ApprovalStatus);
            entity.HasIndex(b => b.Status);
            entity.HasIndex(b => b.IsPremium);
            entity.HasIndex(b => b.ISBN).IsUnique();
            
            // Composite indexes for common queries
            entity.HasIndex(b => new { b.ApprovalStatus, b.Status, b.IsPremium });
            entity.HasIndex(b => new { b.CategoryId, b.ApprovalStatus, b.Status });
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

        // Identity-related configurations
        builder.Entity<AppUser>().ToTable("Users", t => t.ExcludeFromMigrations());
        builder.Entity<AppRole>().ToTable("Roles", t => t.ExcludeFromMigrations());
    }
}