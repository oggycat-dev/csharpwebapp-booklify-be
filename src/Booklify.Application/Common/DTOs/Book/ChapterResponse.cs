using System.Text.Json.Serialization;

namespace Booklify.Application.Common.DTOs.Book;

public class ChapterResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    [JsonPropertyName("order")]
    public int Order { get; set; }
    [JsonPropertyName("href")]
    public string? Href { get; set; }
    [JsonPropertyName("cfi")]
    public string? Cfi { get; set; }
    [JsonPropertyName("parent_chapter_id")]
    public Guid? ParentChapterId { get; set; }
    [JsonPropertyName("child_chapters")]
    public List<ChapterResponse>? ChildChapters { get; set; }
} 