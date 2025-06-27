using System.Text.Json.Serialization;
using Booklify.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Booklify.Application.Common.DTOs.Book;

public class UpdateBookRequest
{
    /// <summary>
    /// Tiêu đề sách
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Mô tả sách
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Tác giả sách
    /// </summary>
    [JsonPropertyName("author")]
    public string? Author { get; set; }

    /// <summary>
    /// Mã ISBN của sách
    /// </summary>
    [JsonPropertyName("isbn")]
    public string? ISBN { get; set; }

    /// <summary>
    /// Nhà xuất bản
    /// </summary>
    [JsonPropertyName("publisher")]
    public string? Publisher { get; set; }

    /// <summary>
    /// ID danh mục sách
    /// </summary>
    [JsonPropertyName("category_id")]
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Trạng thái sách
    /// </summary>
    [JsonPropertyName("status")]
    public EntityStatus? Status { get; set; }

    /// <summary>
    /// Sách có phí hay không
    /// </summary>
    [JsonPropertyName("is_premium")]
    public bool? IsPremium { get; set; }

    /// <summary>
    /// Thẻ tag của sách (phân cách bằng dấu phẩy)
    /// </summary>
    [JsonPropertyName("tags")]
    public string? Tags { get; set; }

    /// <summary>
    /// Ngày xuất bản
    /// </summary>
    [JsonPropertyName("published_date")]
    public DateTime? PublishedDate { get; set; }

    /// <summary>
    /// File sách mới (tùy chọn - để trống nếu không muốn thay đổi file)
    /// </summary>
    [JsonPropertyName("file")]
    public IFormFile? File { get; set; }
} 