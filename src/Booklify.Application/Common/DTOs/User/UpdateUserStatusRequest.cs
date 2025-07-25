using System.Text.Json.Serialization;

namespace Booklify.Application.Common.DTOs.User;
 
public class UpdateUserStatusRequest
{
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
} 