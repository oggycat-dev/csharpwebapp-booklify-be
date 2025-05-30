namespace Booklify.Infrastructure.Models;

/// <summary>
/// Settings for JWT configuration
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Secret key for signing tokens
    /// </summary>
    public string Secret { get; set; } = string.Empty;
    
    /// <summary>
    /// Token issuer
    /// </summary>
    public string Issuer { get; set; } = string.Empty;
    
    /// <summary>
    /// Token audience(s), comma separated
    /// </summary>
    public string? Audience { get; set; }
    
    /// <summary>
    /// Token expiration time in minutes
    /// </summary>
    public int ExpiresInMinutes { get; set; } = 60;
    
    /// <summary>
    /// Refresh token expiration time in days
    /// </summary>
    public int RefreshTokenExpiresInDays { get; set; } = 7;
} 