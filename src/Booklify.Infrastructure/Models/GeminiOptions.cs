namespace Booklify.Infrastructure.Models;

/// <summary>
/// Configuration options for Gemini AI service
/// </summary>
public class GeminiOptions
{
    /// <summary>
    /// Gemini API Key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gemini API Base URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";

    /// <summary>
    /// Model name to use
    /// </summary>
    public string Model { get; set; } = "gemini-pro";

    /// <summary>
    /// Maximum tokens to generate
    /// </summary>
    public int MaxTokens { get; set; } = 2048;

    /// <summary>
    /// Temperature for response generation
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Whether the service is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
} 