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

        // Identity-related configurations
        builder.Entity<AppUser>().ToTable("Users", t => t.ExcludeFromMigrations());
        builder.Entity<AppRole>().ToTable("Roles", t => t.ExcludeFromMigrations());
    }
}