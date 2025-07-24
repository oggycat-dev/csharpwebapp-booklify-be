using System.Text.Json.Serialization;
using Booklify.Application.Common.DTOs.User;
using Booklify.Application.Common.DTOs.Book;

namespace Booklify.Application.Common.DTOs.Statistics
{
    public class AdminStatisticsDto
    {
        [JsonPropertyName("total_users")]
        public int TotalUsers { get; set; }
        [JsonPropertyName("total_books")]
        public int TotalBooks { get; set; }
        [JsonPropertyName("total_premium_users")]
        public int TotalPremiumUsers { get; set; }
        [JsonPropertyName("total_book_reads")]
        public int TotalBookReads { get; set; }
        [JsonPropertyName("pending_books")]
        public List<BookListItemResponse> PendingBooks { get; set; } = new();
        [JsonPropertyName("recent_users")]
        public List<UserResponse> RecentUsers { get; set; } = new();
        [JsonPropertyName("recent_books")]
        public List<BookListItemResponse> RecentBooks { get; set; } = new();
    }
} 