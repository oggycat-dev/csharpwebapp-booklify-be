using System.Text.Json.Serialization;

namespace Booklify.Application.Common.DTOs.Auth;

/// <summary>
/// Response DTO for authentication operations
/// </summary>
public class AuthenticationResponse
{
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
    
    [JsonPropertyName("app_role")]
    public List<string> AppRole { get; set; } = new();
    
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
    
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;
    
    [JsonPropertyName("token_expires_in")]
    public int TokenExpiresIn { get; set; }
    
    [JsonPropertyName("avatar")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Avatar { get; set; }
    
} 