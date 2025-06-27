using System.Text.Json.Serialization;
using Booklify.Application.Common.Interfaces;

namespace Booklify.Application.Common.DTOs.BookAI;

public class ChapterAIResponse
{
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
    
    [JsonPropertyName("translation")]
    public string? Translation { get; set; }
    
    [JsonPropertyName("keywords")]
    public List<string>? Keywords { get; set; }
    
    [JsonPropertyName("flashcards")]
    public List<FlashcardDto>? Flashcards { get; set; }
    
    [JsonPropertyName("processed_actions")]
    public List<string> ProcessedActions { get; set; } = new();
    
    [JsonPropertyName("chapter_title")]
    public string ChapterTitle { get; set; } = string.Empty;
    
    [JsonPropertyName("processing_time_ms")]
    public long ProcessingTimeMs { get; set; }
}

public class ChapterInfo
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("order")]
    public int Order { get; set; }
    
    [JsonPropertyName("has_ai_result")]
    public bool HasAIResult { get; set; }
} 