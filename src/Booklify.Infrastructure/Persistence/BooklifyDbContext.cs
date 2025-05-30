using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Booklify.Domain.Entities;
using Booklify.Domain.Entities.Identity;
using Booklify.Domain.Commons;
using System.Linq.Expressions;
using Booklify.Application.Common.Interfaces;

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

            entity.Property(u => u.Gender)
                .HasConversion<int>();
                
            // Add index for IdentityUserId
            entity.HasIndex(u => u.IdentityUserId)
                .IsUnique()
                .HasFilter("[IdentityUserId] IS NOT NULL");
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

            entity.Property(s => s.Gender)
                .HasConversion<int>();
                
            // Add index for IdentityUserId
            entity.HasIndex(s => s.IdentityUserId)
                .IsUnique()
                .HasFilter("[IdentityUserId] IS NOT NULL");
        });

        // Identity-related configurations
        builder.Entity<AppUser>().ToTable("Users", t => t.ExcludeFromMigrations());
        builder.Entity<AppRole>().ToTable("Roles", t => t.ExcludeFromMigrations());
    }
}