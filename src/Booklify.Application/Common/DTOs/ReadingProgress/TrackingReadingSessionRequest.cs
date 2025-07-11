using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Booklify.Application.Common.DTOs.ReadingProgress;

/// <summary>
/// Request để track chapter access, position, and completion
/// </summary>
public class TrackingReadingSessionRequest
{
    /// <summary>
    /// ID của sách đang đọc
    /// </summary>
    /// <example>123e4567-e89b-12d3-a456-426614174000</example>
    [JsonPropertyName("book_id")]
    public Guid BookId { get; set; }

    /// <summary>
    /// ID của chapter user đang access
    /// </summary>
    /// <example>123e4567-e89b-12d3-a456-426614174001</example>
    [JsonPropertyName("chapter_id")]
    public Guid ChapterId { get; set; }

    /// <summary>
    /// Current CFI position trong chapter này (optional)
    /// </summary>
    /// <example>epubcfi(/6/4[chapter1]!/4/2/1:0)</example>
    [JsonPropertyName("current_cfi")]
    public string? CurrentCfi { get; set; }

    /// <summary>
    /// Frontend xác nhận chapter này đã hoàn thành chưa
    /// 
    /// **Business Rule**: Chapter completion is IMMUTABLE
    /// - false → true: OK (mark as completed)
    /// - true → false: IGNORED (cannot revert completion)
    /// 
    /// Once a chapter is completed, it cannot be reverted back to incomplete state.
    /// </summary>
    [JsonPropertyName("is_completed")]
    public bool IsCompleted { get; set; } = false;
} 