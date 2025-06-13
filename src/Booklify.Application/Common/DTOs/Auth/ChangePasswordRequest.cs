using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Booklify.Application.Common.DTOs.Auth;

/// <summary>
/// Request DTO for changing password
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// Current password
    /// </summary>
    [JsonPropertyName("old_password")]
    [Required(ErrorMessage = "Old password is required")]
    public string OldPassword { get; set; } = string.Empty;
    
    /// <summary>
    /// New password
    /// </summary>
    [JsonPropertyName("new_password")]
    [Required(ErrorMessage = "New password is required")]
    [MinLength(6, ErrorMessage = "New password must be at least 6 characters")]
    public string NewPassword { get; set; } = string.Empty;
} 