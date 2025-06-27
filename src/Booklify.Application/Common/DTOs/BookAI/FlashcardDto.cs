using System.Text.Json.Serialization;

namespace Booklify.Application.Common.DTOs.BookAI;

/// <summary>
/// DTO for flashcard data
/// </summary>
public class FlashcardDto
{
    /// <summary>
    /// Từ vựng tiếng Anh
    /// </summary>
    [JsonPropertyName("word")]
    public string Word { get; set; } = "";

    /// <summary>
    /// Nghĩa tiếng Việt
    /// </summary>
    [JsonPropertyName("meaning")]
    public string Meaning { get; set; } = "";

    /// <summary>
    /// Ví dụ câu sử dụng
    /// </summary>
    [JsonPropertyName("example")]
    public string Example { get; set; } = "";

    /// <summary>
    /// Định nghĩa tiếng Anh
    /// </summary>
    [JsonPropertyName("definition")]
    public string Definition { get; set; } = "";

    /// <summary>
    /// Loại từ (noun, verb, adjective, etc.)
    /// </summary>
    [JsonPropertyName("partOfSpeech")]
    public string PartOfSpeech { get; set; } = "";

    /// <summary>
    /// Mức độ khó (1-5)
    /// </summary>
    [JsonPropertyName("difficultyLevel")]
    public int DifficultyLevel { get; set; } = 1;
} 