using Microsoft.AspNetCore.Http;
using Booklify.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Booklify.Application.Common.DTOs.Book;

public class CreateBookRequest
{
    /// <summary>
    /// ID danh mục sách
    /// </summary>
    [JsonPropertyName("category_id")]
    [Required(ErrorMessage = "Danh mục sách là bắt buộc")]
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Sách có phí hay không
    /// </summary>
    [JsonPropertyName("is_premium")]
    public bool IsPremium { get; set; } = false;

    /// <summary>
    /// Thẻ tag của sách (phân cách bằng dấu phẩy)
    /// </summary>
    [JsonPropertyName("tags")]
    [StringLength(500, ErrorMessage = "Tags không được vượt quá 500 ký tự")]
    public string? Tags { get; set; }

    /// <summary>
    /// ISBN sách (tùy chọn)
    /// </summary>
    [JsonPropertyName("isbn")]
    [StringLength(20, ErrorMessage = "ISBN không được vượt quá 20 ký tự")]
    public string? Isbn { get; set; }

    /// <summary>
    /// File sách (EPUB)
    /// </summary>
    [JsonPropertyName("file")]
    [Required(ErrorMessage = "File sách là bắt buộc")]
    public IFormFile File { get; set; } = null!;
} 