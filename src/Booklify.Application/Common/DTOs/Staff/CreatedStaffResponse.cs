namespace Booklify.Application.Common.DTOs.Staff;

using System.Text.Json.Serialization;
using Booklify.Domain.Enums;

public class CreatedStaffResponse
{
    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = string.Empty;
    [JsonPropertyName("last_name")]
    public string LastName { get; set; } = string.Empty;
    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;
    [JsonPropertyName("staff_code")]
    public string StaffCode { get; set; } = string.Empty;
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;
    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;
    [JsonPropertyName("position")]
    public string Position { get; set; } = string.Empty;
}