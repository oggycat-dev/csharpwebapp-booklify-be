using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Booklify.Domain.Entities;
using Booklify.Domain.Entities.Identity;
using Booklify.Application.Common.Interfaces;

namespace Booklify.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<AppUser, AppRole, string>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        // Disable FirstWithoutOrderByAndFilterWarning
        optionsBuilder.ConfigureWarnings(warnings => 
            warnings.Ignore(CoreEventId.FirstWithoutOrderByAndFilterWarning));
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        
        
        // Customize Identity tables
        builder.Entity<AppUser>(entity =>
        {
            entity.ToTable(name: "Users");
            
            // Configure EntityId
            entity.Property(u => u.EntityId)
                .IsRequired(false);
                
            // Configure navigation properties without relationships
            entity.Navigation(u => u.UserProfile).AutoInclude();
            entity.Navigation(u => u.StaffProfile).AutoInclude();
            
            // Add indexes
            entity.HasIndex(u => u.EntityId)
                .IsUnique()
                .HasFilter("[EntityId] IS NOT NULL");
                
            entity.HasIndex(u => u.NormalizedUserName)
                .HasDatabaseName("UserNameIndex")
                .IsUnique();
                
            entity.HasIndex(u => u.NormalizedEmail)
                .HasDatabaseName("EmailIndex");
        });

        builder.Entity<AppRole>(entity =>
        {
            entity.ToTable(name: "Roles");
            
            entity.HasIndex(r => r.NormalizedName)
                .HasDatabaseName("RoleNameIndex")
                .IsUnique();
        });

        builder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(r => new { r.UserId, r.RoleId });
            
            // Add index for better query performance
            entity.HasIndex(r => new { r.UserId, r.RoleId });
        });

        builder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.ToTable("UserClaims");
        });

        builder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.ToTable("UserLogins");
            entity.HasKey(l => new { l.LoginProvider, l.ProviderKey });
        });

        builder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.ToTable("RoleClaims");
        });

        builder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.ToTable("UserTokens");
            entity.HasKey(t => new { t.UserId, t.LoginProvider, t.Name });
        });

        // Ignore entities that are configured in the BooklifyDbContext
        builder.Ignore<UserProfile>();
        builder.Ignore<StaffProfile>();
    }
} 