using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Booklify.Domain.Entities;
using Booklify.Domain.Entities.Identity;
using Booklify.Domain.Enums;

namespace Booklify.Infrastructure.Persistence;

public class SeedTracking
{
    public Guid Id { get; set; }
    public bool IsSeeded { get; set; }
    public DateTime SeededAt { get; set; }
}

public static class DbInitializer
{
    private static async Task<bool> ExecuteSafelyAsync(Func<Task> operation, string operationName)
    {
        try
        {
            await operation();
            Console.WriteLine($"Successfully completed: {operationName}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in {operationName}: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            return false;
        }
    }
    
    private static async Task SeedRolesAsync(RoleManager<AppRole> roleManager)
    {
        try
        {
            Console.WriteLine("Checking for new roles to add...");
            
            var existingRoles = await roleManager.Roles.Select(r => r.Name).ToListAsync();
            
            var allRoles = new List<AppRole>
            {
                new AppRole { Name = Role.Admin.ToString(), NormalizedName = Role.Admin.ToString().ToUpper(), Description = "System Administrator", IsSystemRole = true },
                new AppRole { Name = Role.Staff.ToString(), NormalizedName = Role.Staff.ToString().ToUpper(), Description = "Staff Member", IsSystemRole = true },
                new AppRole { Name = Role.User.ToString(), NormalizedName = Role.User.ToString().ToUpper(), Description = "Regular User", IsSystemRole = true },
            };

            int addedRoles = 0;
            foreach (var role in allRoles)
            {
                if (!existingRoles.Contains(role.Name))
                {
                    var result = await roleManager.CreateAsync(role);
                    if (result.Succeeded)
                    {
                        Console.WriteLine($"Added new role: {role.Name}");
                        addedRoles++;
                    }
                    else
                    {
                        Console.WriteLine($"Failed to add role {role.Name}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
            
            Console.WriteLine($"Role seeding completed. Added {addedRoles} new roles.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding roles: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
    }
    
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var dbContext = services.GetRequiredService<BooklifyDbContext>();
            
            bool shouldSeed = await ShouldPerformSeedingAsync(dbContext);
            
            await SeedRolesAsync(services.GetRequiredService<RoleManager<AppRole>>());
            
            if (!shouldSeed)
            {
                Console.WriteLine("Database is already seeded. Skipping core seed operation.");
                return;
            }
            
            Console.WriteLine("Starting database initialization...");
            
            Console.WriteLine("Testing database connection...");
            if (!await dbContext.Database.CanConnectAsync())
            {
                Console.WriteLine("Cannot connect to database. Please check your connection string and ensure the database server is running.");
                return;
            }
            Console.WriteLine("Database connection successful.");

            #region Seed Admin Users
            try
            {
                var userManager = services.GetRequiredService<UserManager<AppUser>>();
                
                var adminEmail = "admin@booklify.com";
                var adminExists = await userManager.FindByNameAsync(adminEmail) != null;

                if (!adminExists)
                {
                    Console.WriteLine("Creating Admin user...");
                    var adminUser = new AppUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        NormalizedEmail = adminEmail.ToUpper(),
                        NormalizedUserName = adminEmail.ToUpper(),
                        EmailConfirmed = true,
                        PhoneNumber = "0901234567",
                        PhoneNumberConfirmed = true,
                        EntityId = Guid.NewGuid()
                    };

                    var result = await userManager.CreateAsync(adminUser, "Admin@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, Role.Admin.ToString());
                        
                        // Create admin staff profile
                        var adminProfile = new StaffProfile
                        {
                            Id = adminUser.EntityId.Value,
                            IdentityUserId = adminUser.Id,
                            FirstName = "Admin",
                            LastName = "Booklify",
                            FullName = "Admin Booklify",
                            JoinDate = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = adminUser.EntityId.Value
                        };
                        
                        await dbContext.StaffProfiles.AddAsync(adminProfile);
                        await dbContext.SaveChangesAsync();
                        
                        Console.WriteLine("Admin user and profile created successfully");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to create Admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating admin users: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
            #endregion
            
            await MarkDatabaseAsSeededAsync(dbContext);
            
            Console.WriteLine("Database initialization completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Critical error in database initialization: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }

    private static async Task<bool> ShouldPerformSeedingAsync(BooklifyDbContext dbContext)
    {
        var seedTracking = await dbContext.Set<SeedTracking>().FirstOrDefaultAsync();
        
        if (seedTracking != null && seedTracking.IsSeeded)
        {
            return false;
        }
        
        // Check both profile types
        int userCount = await dbContext.UserProfiles.CountAsync();
        int staffCount = await dbContext.StaffProfiles.CountAsync();
        
        if (userCount > 0 || staffCount > 0)
        {
            if (seedTracking == null)
            {
                seedTracking = new SeedTracking
                {
                    Id = Guid.NewGuid(),
                    IsSeeded = true,
                    SeededAt = DateTime.UtcNow
                };
                await dbContext.Set<SeedTracking>().AddAsync(seedTracking);
            }
            else
            {
                seedTracking.IsSeeded = true;
                seedTracking.SeededAt = DateTime.UtcNow;
            }
            await dbContext.SaveChangesAsync();
            
            return false;
        }
        
        return true;
    }
    
    private static async Task MarkDatabaseAsSeededAsync(BooklifyDbContext dbContext)
    {
        var seedTracking = await dbContext.Set<SeedTracking>().FirstOrDefaultAsync();
        
        if (seedTracking == null)
        {
            seedTracking = new SeedTracking
            {
                Id = Guid.NewGuid(),
                IsSeeded = true,
                SeededAt = DateTime.UtcNow
            };
            await dbContext.Set<SeedTracking>().AddAsync(seedTracking);
        }
        else
        {
            seedTracking.IsSeeded = true;
            seedTracking.SeededAt = DateTime.UtcNow;
        }
        
        await dbContext.SaveChangesAsync();
        Console.WriteLine("Database marked as seeded successfully");
    }
} 