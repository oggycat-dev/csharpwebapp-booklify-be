using System.Text.Json.Serialization;

namespace Booklify.Application.Common.DTOs.Book;

/// <summary>
/// Request model for resubmitting a rejected book for approval
/// </summary>
public class ResubmitBookRequest
{
    /// <summary>
    /// Optional note explaining why the book is being resubmitted
    /// </summary>
    [JsonPropertyName("resubmit_note")]
    public string? ResubmitNote { get; set; }
} 