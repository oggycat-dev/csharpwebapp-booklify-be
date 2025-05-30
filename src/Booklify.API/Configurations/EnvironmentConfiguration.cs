namespace Booklify.API.Configurations;

public static class EnvironmentConfiguration
{
    public static WebApplicationBuilder AddEnvironmentConfiguration(this WebApplicationBuilder builder)
    {
        var environment = builder.Environment.EnvironmentName;
        
        // Load appropriate .env file based on environment
        if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
        {
            DotNetEnv.Env.Load(".env.production");
            builder.Configuration["App:Environment"] = "Production";
        }
        else
        {
            DotNetEnv.Env.Load();
            builder.Configuration["App:Environment"] = environment;
        }
        
        // Load environment variables into Configuration
        
        // Add new environment variables
        builder.Configuration["Security:RequireHttps"] = 
            Environment.GetEnvironmentVariable("REQUIRE_HTTPS") ?? "false";
        builder.Configuration["Cors:AllowAnyOrigin"] = 
            Environment.GetEnvironmentVariable("CORS_ALLOW_ANY_ORIGIN") ?? "true";
        
        // Database Connection
        builder.Configuration["ConnectionStrings:DefaultConnection"] = 
            Environment.GetEnvironmentVariable("CONNECTION_STRING");
            
        // JWT Settings    
        builder.Configuration["Jwt:Secret"] = 
            Environment.GetEnvironmentVariable("JWT_SECRET");
        builder.Configuration["Jwt:Issuer"] = 
            Environment.GetEnvironmentVariable("JWT_ISSUER");
        builder.Configuration["Jwt:Audience"] = 
            Environment.GetEnvironmentVariable("JWT_AUDIENCE");
        builder.Configuration["Jwt:ExpiresInMinutes"] = 
            Environment.GetEnvironmentVariable("JWT_EXPIRES_IN_MINUTES");
        builder.Configuration["Jwt:RefreshTokenExpiresInDays"] = 
            Environment.GetEnvironmentVariable("REFRESH_TOKEN_EXPIRES_IN_DAYS");

        // Frontend URL
        builder.Configuration["FrontendUrl"] = 
            Environment.GetEnvironmentVariable("FRONTEND_URL");
            
        // Load allowed origins for CORS from FRONTEND_URL
        string corsOriginsString = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? string.Empty;
        if (!string.IsNullOrEmpty(corsOriginsString))
        {
            var origins = corsOriginsString.Split(',', StringSplitOptions.RemoveEmptyEntries);
            builder.Configuration["Cors:AllowedOrigins"] = string.Join(',', origins);
        }

        // System Settings
        builder.Configuration["SystemName"] = 
            Environment.GetEnvironmentVariable("SYSTEM_NAME") ?? "Booklify";

        // Storage Settings
        builder.Configuration["Storage:ProviderType"] = 
            Environment.GetEnvironmentVariable("STORAGE_PROVIDER_TYPE") ?? "LocalStorage";
        builder.Configuration["Storage:BaseUrl"] = 
            Environment.GetEnvironmentVariable("STORAGE_BASE_URL") ?? string.Empty;

        return builder;
    }
} 