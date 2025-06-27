using Booklify.Infrastructure.Models;
using Microsoft.Extensions.Options;

namespace Booklify.API.Configurations;

/// <summary>
/// Configuration for Gemini AI service
/// </summary>
public static class GeminiConfiguration
{
    /// <summary>
    /// Configure Gemini AI service with environment variables and settings
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddGeminiConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure GeminiOptions with environment variables priority
        services.Configure<GeminiOptions>(options =>
        {
            // Load from appsettings.json first (if exists)
            configuration.GetSection("Gemini").Bind(options);

            // Override with environment variables (higher priority)
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? 
                        Environment.GetEnvironmentVariable("GEMINI__APIKEY");
            if (!string.IsNullOrEmpty(apiKey))
            {
                options.ApiKey = apiKey;
            }

            var baseUrl = Environment.GetEnvironmentVariable("GEMINI_BASE_URL") ?? 
                         Environment.GetEnvironmentVariable("GEMINI__BASEURL");
            if (!string.IsNullOrEmpty(baseUrl))
            {
                options.BaseUrl = baseUrl;
            }

            var model = Environment.GetEnvironmentVariable("GEMINI_MODEL") ?? 
                       Environment.GetEnvironmentVariable("GEMINI__MODEL");
            if (!string.IsNullOrEmpty(model))
            {
                options.Model = model;
            }

            var maxTokensStr = Environment.GetEnvironmentVariable("GEMINI_MAX_TOKENS") ?? 
                              Environment.GetEnvironmentVariable("GEMINI__MAXTOKENS");
            if (!string.IsNullOrEmpty(maxTokensStr) && int.TryParse(maxTokensStr, out var maxTokens))
            {
                options.MaxTokens = maxTokens;
            }

            var temperatureStr = Environment.GetEnvironmentVariable("GEMINI_TEMPERATURE") ?? 
                                Environment.GetEnvironmentVariable("GEMINI__TEMPERATURE");
            if (!string.IsNullOrEmpty(temperatureStr) && double.TryParse(temperatureStr, out var temperature))
            {
                options.Temperature = temperature;
            }

            var isEnabledStr = Environment.GetEnvironmentVariable("GEMINI_IS_ENABLED") ?? 
                              Environment.GetEnvironmentVariable("GEMINI__ISENABLED");
            if (!string.IsNullOrEmpty(isEnabledStr) && bool.TryParse(isEnabledStr, out var isEnabled))
            {
                options.IsEnabled = isEnabled;
            }

            // Set defaults if not configured
            if (string.IsNullOrEmpty(options.BaseUrl))
            {
                options.BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";
            }

            if (string.IsNullOrEmpty(options.Model))
            {
                options.Model = "gemini-1.5-flash";
            }

            if (options.MaxTokens <= 0)
            {
                options.MaxTokens = 2048;
            }

            if (options.Temperature <= 0)
            {
                options.Temperature = 0.7;
            }
        });

        return services;
    }

    /// <summary>
    /// Validate Gemini configuration
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder</returns>
    public static IApplicationBuilder ValidateGeminiConfiguration(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var geminiOptions = scope.ServiceProvider.GetRequiredService<IOptions<GeminiOptions>>().Value;

        // Log configuration status
        logger.LogInformation("Gemini Configuration:");
        logger.LogInformation("- Enabled: {IsEnabled}", geminiOptions.IsEnabled);
        logger.LogInformation("- Model: {Model}", geminiOptions.Model);
        logger.LogInformation("- Base URL: {BaseUrl}", geminiOptions.BaseUrl);
        logger.LogInformation("- Max Tokens: {MaxTokens}", geminiOptions.MaxTokens);
        logger.LogInformation("- Temperature: {Temperature}", geminiOptions.Temperature);

        if (string.IsNullOrEmpty(geminiOptions.ApiKey))
        {
            logger.LogWarning("Gemini API Key is not configured! Please set GEMINI_API_KEY environment variable.");
        }
        else
        {
            var maskedKey = geminiOptions.ApiKey.Length > 8 ? 
                geminiOptions.ApiKey.Substring(0, 8) + "..." : 
                "***";
            logger.LogInformation("- API Key: {ApiKey}", maskedKey);
        }

        return app;
    }
} 