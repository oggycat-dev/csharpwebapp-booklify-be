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

using Amazon.S3;
using Booklify.Infrastructure.Services.BackgroundJobs;


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
            options.User.RequireUniqueEmail = true;  // Changed to true for email verification
            
            // SignIn settings  
            options.SignIn.RequireConfirmedEmail = true;   // Changed to true for email verification
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();
        
        // Configure email token lifespan (24 hours)
        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromHours(24);
        });
        
        // Configure JWT settings
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        
        // Configure Storage settings
        services.Configure<StorageSettings>(configuration.GetSection("Storage"));
        
        // Configure VNPay settings
        services.Configure<VNPaySettings>(configuration.GetSection("VNPay"));
        
        // Configure Email settings
        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        
        // Gemini configuration is now handled in API layer
        
        // Register repositories
        services.AddScoped<IStaffProfileRepository, StaffProfileRepository>();
        services.AddScoped<IBookCategoryRepository, BookCategoryRepository>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IChapterRepository, ChapterRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IFileInfoRepository, FileInfoRepository>();
        services.AddScoped<IChapterAIResultRepository, ChapterAIResultRepository>();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Configure AWS S3 Client
        services.AddScoped<IAmazonS3>(provider =>
        {
            var storageSettings = configuration.GetSection("Storage").Get<StorageSettings>();
            var s3Config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(storageSettings?.AmazonS3?.Region ?? "us-east-1"),
                UseHttp = !(storageSettings?.AmazonS3?.UseHttps ?? true)
            };
            
            var accessKey = storageSettings?.AmazonS3?.AccessKey ?? Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            var secretKey = storageSettings?.AmazonS3?.SecretKey ?? Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
            
            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            {
                return new AmazonS3Client(accessKey, secretKey, s3Config);
            }
            
            // Use default credential chain (IAM roles, environment variables, etc.)
            return new AmazonS3Client(s3Config);
        });

        // Register storage services
        services.AddScoped<LocalStorageService>();
        services.AddScoped<AmazonS3StorageService>();
        services.AddScoped<IStorageFactory, StorageFactory>();
        services.AddScoped<IStorageService>(provider => 
            provider.GetRequiredService<IStorageFactory>().CreateStorageService());

        // Register services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IEPubService, EPubService>();
        services.AddScoped<IFileBackgroundService, FileBackgroundService>();
        services.AddScoped<IFileService, FileService>();
        
        // Register Gemini service with HttpClient
        services.AddHttpClient<GeminiService>();
        services.AddScoped<ITextAIService, GeminiService>();
        
        // Background jobs are registered in HangfireConfiguration
        
        
        // Register Hangfire initialization service
        services.AddHostedService<HangfireInitializationService>();
        services.AddScoped<IVNPayService, VNPayService>();
        
        // Register Email service
        services.AddScoped<IEmailService, EmailService>();
            
        return services;
    }
}