using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Booklify.Application.Common.DTOs.Auth;

/// <summary>
/// Request DTO for user registration
/// </summary>
public class UserRegistrationRequest
{
    [JsonPropertyName("username")]
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Valid email is required")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("password")]
    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;
    
    [JsonPropertyName("phone_number")]
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Response DTO for user registration
/// </summary>
public class UserRegistrationResponse
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for staff registration
/// </summary>
public class StaffRegistrationRequest : UserRegistrationRequest
{
    [JsonPropertyName("full_name")]
    [Required(ErrorMessage = "Full name is required")]
    public string FullName { get; set; } = string.Empty;
    
    [JsonPropertyName("employee_id")]
    public string? EmployeeId { get; set; }
}

/// <summary>
/// Response DTO for staff registration
/// </summary>
public class StaffRegistrationResponse : UserRegistrationResponse
{
    [JsonPropertyName("staff_id")]
    public Guid? StaffId { get; set; }
} 