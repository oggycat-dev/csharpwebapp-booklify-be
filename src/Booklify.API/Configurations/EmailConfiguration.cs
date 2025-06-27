using Microsoft.AspNetCore.Identity;

namespace Booklify.API.Configurations;

/// <summary>
/// Extension methods for Email configuration
/// </summary>
public static class EmailConfiguration
{
    /// <summary>
    /// Configure Email services (SMTP/MailKit)
    /// </summary>
    public static IServiceCollection AddEmailConfiguration(this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Get SMTP email settings from configuration
        var smtpHost = configuration["Email:Host"];
        var smtpPort = configuration["Email:Port"];
        var smtpUsername = configuration["Email:Username"];
        var smtpPassword = configuration["Email:Password"];
        var fromEmail = configuration["Email:FromEmail"];
        var fromName = configuration["Email:FromName"];

        if (string.IsNullOrEmpty(smtpHost))
            throw new ArgumentNullException(nameof(smtpHost), "SMTP Host is not configured");

        if (string.IsNullOrEmpty(smtpUsername))
            throw new ArgumentNullException(nameof(smtpUsername), "SMTP Username is not configured");

        if (string.IsNullOrEmpty(smtpPassword))
            throw new ArgumentNullException(nameof(smtpPassword), "SMTP Password is not configured");

        if (string.IsNullOrEmpty(fromEmail))
            throw new ArgumentNullException(nameof(fromEmail), "SMTP From Email is not configured");

        if (string.IsNullOrEmpty(fromName))
            throw new ArgumentNullException(nameof(fromName), "SMTP From Name is not configured");

        // Configure email token lifespan (24 hours)
        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromHours(24);
        });

        return services;
    }
} 