using System.Text.Json.Serialization;
using Booklify.Domain.Enums;

namespace Booklify.Application.Common.DTOs.Staff;

public class UpdateStaffRequest
{
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }
    
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }
    
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
    
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    
    [JsonPropertyName("address")]
    public string? Address { get; set; }
    
    [JsonPropertyName("position")]
    public StaffPosition? Position { get; set; }
    
    [JsonPropertyName("is_active")]
    public bool? IsActive { get; set; }
} 