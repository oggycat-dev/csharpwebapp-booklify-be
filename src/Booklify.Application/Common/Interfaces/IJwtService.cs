using Booklify.Domain.Entities.Identity;

namespace Booklify.Application.Common.Interfaces;

/// <summary>
/// Service interface for JWT token operations
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generate JWT token for a user
    /// </summary>
    /// <param name="user">User for which to generate the token</param>
    /// <param name="requestOrigin">Optional request origin for audience validation</param>
    /// <returns>Token and roles tuple</returns>
    (string token, List<string> roles) GenerateJwtToken(AppUser user, string? requestOrigin = null);
    
    /// <summary>
    /// Generate refresh token for a user
    /// </summary>
    /// <returns>Refresh token string</returns>
    string GenerateRefreshToken();
    
    /// <summary>
    /// Validate JWT token
    /// </summary>
    /// <param name="token">Token to validate</param>
    /// <returns>True if token is valid, false otherwise</returns>
    bool ValidateToken(string token);
    
    /// <summary>
    /// Get user id from token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>User ID or null if token is invalid</returns>
    string? GetUserIdFromToken(string token);
    
    /// <summary>
    /// Get principal claims from token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>ClaimsPrincipal or null if token is invalid</returns>
    System.Security.Claims.ClaimsPrincipal? GetPrincipalFromToken(string token);
} 