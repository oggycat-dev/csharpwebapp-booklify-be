using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Booklify.Application.Common.DTOs.BookAI;

public class ChapterAIRequest
{
    [JsonPropertyName("actions")]
    [Required]
    public List<string> Actions { get; set; } = new();
    
    [JsonPropertyName("content")]
    [Required]
    public string Content { get; set; } = string.Empty;
} 