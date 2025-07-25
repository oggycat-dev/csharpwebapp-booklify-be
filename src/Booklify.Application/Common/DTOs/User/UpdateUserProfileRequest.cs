using System.Text.Json.Serialization;
using Booklify.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Booklify.Application.Common.DTOs.User;

public class UpdateUserProfileRequest
{
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }
    
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }
    
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
    
    [JsonPropertyName("address")]
    public string? Address { get; set; }
    
    [JsonPropertyName("birthday")]
    public DateTime? Birthday { get; set; }
    
    [JsonPropertyName("gender")]
    public Gender? Gender { get; set; }
    
    [JsonPropertyName("avatar")]
    public IFormFile? Avatar { get; set; }
} 