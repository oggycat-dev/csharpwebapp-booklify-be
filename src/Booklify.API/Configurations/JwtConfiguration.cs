using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Booklify.API.Configurations;

/// <summary>
/// Extension methods for JWT configuration
/// </summary>
public static class JwtConfiguration
{
    /// <summary>
    /// Configure JWT authentication
    /// </summary>
    public static IServiceCollection AddJwtConfiguration(this IServiceCollection services, 
        IConfiguration configuration, bool requireHttps = false)
    {
        // Get JWT settings
        var jwtSecret = configuration["Jwt:Secret"];
        var jwtIssuer = configuration["Jwt:Issuer"];
        var audience1 = configuration["Jwt:Audience1"];
        var audience2 = configuration["Jwt:Audience2"];

        if (string.IsNullOrEmpty(jwtSecret))
            throw new ArgumentNullException(nameof(jwtSecret), "JWT Secret is not configured");

        if (string.IsNullOrEmpty(jwtIssuer))
            throw new ArgumentNullException(nameof(jwtIssuer), "JWT Issuer is not configured");

        // Create array of valid audiences
        var validAudiences = new List<string>();
        if (!string.IsNullOrEmpty(audience1))
            validAudiences.Add(audience1);
        if (!string.IsNullOrEmpty(audience2))
            validAudiences.Add(audience2);

        // Add JWT authentication
        var key = Encoding.UTF8.GetBytes(jwtSecret);
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = requireHttps;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = validAudiences.Count > 0,
                ValidIssuer = jwtIssuer,
                ValidAudiences = validAudiences,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            
            // Add event handlers for JWT bearer events
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Add("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                }
            };
        });
        
        return services;
    }
} 