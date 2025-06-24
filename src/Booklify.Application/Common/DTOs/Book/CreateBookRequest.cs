using Microsoft.AspNetCore.Http;
using Booklify.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Booklify.Application.Common.DTOs.Book;

public class CreateBookRequest
{
    /// <summary>
    /// Tiêu đề sách
    /// </summary>
    [JsonPropertyName("title")]
    [Required(ErrorMessage = "Tiêu đề sách là bắt buộc")]
    [StringLength(500, ErrorMessage = "Tiêu đề sách không được vượt quá 500 ký tự")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả sách
    /// </summary>
    [JsonPropertyName("description")]
    [StringLength(2000, ErrorMessage = "Mô tả sách không được vượt quá 2000 ký tự")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Tác giả sách
    /// </summary>
    [JsonPropertyName("author")]
    [Required(ErrorMessage = "Tác giả là bắt buộc")]
    [StringLength(200, ErrorMessage = "Tên tác giả không được vượt quá 200 ký tự")]
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Mã ISBN của sách
    /// </summary>
    [JsonPropertyName("isbn")]
    [StringLength(20, ErrorMessage = "Mã ISBN không được vượt quá 20 ký tự")]
    public string ISBN { get; set; } = string.Empty;

    /// <summary>
    /// Nhà xuất bản
    /// </summary>
    [JsonPropertyName("publisher")]
    [StringLength(200, ErrorMessage = "Tên nhà xuất bản không được vượt quá 200 ký tự")]
    public string Publisher { get; set; } = string.Empty;

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
    /// Ngày xuất bản
    /// </summary>
    [JsonPropertyName("published_date")]
    public DateTime? PublishedDate { get; set; }

    /// <summary>
    /// File sách (PDF, EPUB, DOCX, TXT)
    /// </summary>
    [JsonPropertyName("file")]
    [Required(ErrorMessage = "File sách là bắt buộc")]
    public IFormFile File { get; set; } = null!;
} 