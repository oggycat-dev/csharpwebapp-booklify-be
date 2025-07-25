using System.Text.Json.Serialization;

namespace Booklify.Application.Common.DTOs.Book;

/// <summary>
/// Response DTO for book statistics
/// </summary>
public class BookStatisticsResponse
{
    /// <summary>
    /// Số lượng sách chờ duyệt (ApprovalStatus = 0)
    /// </summary>
    [JsonPropertyName("pending_count")]
    public int PendingCount { get; set; }

    /// <summary>
    /// Số lượng sách đã duyệt (ApprovalStatus = 1)
    /// </summary>
    [JsonPropertyName("approved_count")]
    public int ApprovedCount { get; set; }

    /// <summary>
    /// Số lượng sách bị từ chối (ApprovalStatus = 2)
    /// </summary>
    [JsonPropertyName("rejected_count")]
    public int RejectedCount { get; set; }

    /// <summary>
    /// Tổng số sách
    /// </summary>
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Số lượng sách active
    /// </summary>
    [JsonPropertyName("active_count")]
    public int ActiveCount { get; set; }

    /// <summary>
    /// Số lượng sách inactive
    /// </summary>
    [JsonPropertyName("inactive_count")]
    public int InactiveCount { get; set; }

    /// <summary>
    /// Số lượng sách premium
    /// </summary>
    [JsonPropertyName("premium_count")]
    public int PremiumCount { get; set; }

    /// <summary>
    /// Số lượng sách free
    /// </summary>
    [JsonPropertyName("free_count")]
    public int FreeCount { get; set; }
} 