using System.Text.Json.Serialization;
using Booklify.Domain.Enums;

namespace Booklify.Application.Common.DTOs.ChapterNote;

public class ChapterNoteListItemResponse
{
    [JsonPropertyName("cfi")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Cfi { get; set; }
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("page_number")]
    public int PageNumber { get; set; }

    [JsonPropertyName("note_type")]
    public ChapterNoteType NoteType { get; set; }

    [JsonPropertyName("note_type_name")]
    public string NoteTypeName { get; set; }
} 