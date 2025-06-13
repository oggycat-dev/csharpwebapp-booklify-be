using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Booklify.Application.Common.Interfaces;
using Booklify.Domain.Entities.Identity;
using Booklify.Infrastructure.Persistence;
using Booklify.Infrastructure.Services;
using Booklify.Infrastructure.Models;
using Booklify.Application.Common.Interfaces.Repositories;
using Booklify.Infrastructure.Repositories;

namespace Booklify.Infrastructure;

/// <summary>
/// Dependency injection configuration for the Infrastructure layer
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add infrastructure services to the dependency container
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        }
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string not found. Please set the CONNECTION_STRING environment variable or configure it in appsettings.json");
        }
        
        // Configure database contexts
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString,
                sqlOptions => 
                {
                    sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                }));
            
        services.AddDbContext<BooklifyDbContext>(options =>
            options.UseSqlServer(connectionString,
                sqlOptions => 
                {
                    sqlOptions.MigrationsAssembly(typeof(BooklifyDbContext).Assembly.FullName);
                }));
                
        // Register contexts as interfaces
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IBooklifyDbContext>(provider => provider.GetRequiredService<BooklifyDbContext>());
            
        // Configure Identity
        services.AddIdentity<AppUser, AppRole>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
            
            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            
            // User settings
            options.User.RequireUniqueEmail = false;
            
            // SignIn settings
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();
        
        // Configure JWT settings
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        
        // Register repositories
        services.AddScoped<IStaffProfileRepository, StaffProfileRepository>();
        services.AddScoped<IBookCategoryRepository, BookCategoryRepository>();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IIdentityService, IdentityService>();
            
        return services;
    }
}