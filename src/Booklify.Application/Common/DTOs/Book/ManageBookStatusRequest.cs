using System.Text.Json.Serialization;
using Booklify.Domain.Enums;

namespace Booklify.Application.Common.DTOs.Book;

public class ManageBookStatusRequest
{
    /// <summary>
    /// Trạng thái phê duyệt sách (0: Pending, 1: Approved, 2: Rejected)
    /// </summary>
    [JsonPropertyName("approval_status")]
    public ApprovalStatus? ApprovalStatus { get; set; }

    /// <summary>
    /// Ghi chú phê duyệt (bắt buộc khi từ chối)
    /// </summary>
    [JsonPropertyName("approval_note")]
    public string? ApprovalNote { get; set; }

    /// <summary>
    /// Sách có phí hay không
    /// </summary>
    [JsonPropertyName("is_premium")]
    public bool? IsPremium { get; set; }
} 