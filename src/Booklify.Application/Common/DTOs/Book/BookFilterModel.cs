using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;

namespace Booklify.Application.Common.DTOs.Book;

/// <summary>
/// Mô hình lọc danh sách sách
/// </summary>
public class BookFilterModel : FilterBase
{
    /// <summary>
    /// Lọc theo tiêu đề sách (chứa)
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// Lọc theo tác giả (chứa)
    /// </summary>
    public string? Author { get; set; }
    
    /// <summary>
    /// Lọc theo ISBN (chứa)
    /// </summary>
    public string? ISBN { get; set; }
    
    /// <summary>
    /// Lọc theo nhà xuất bản (chứa)
    /// </summary>
    public string? Publisher { get; set; }
    
    /// <summary>
    /// Lọc theo danh mục sách
    /// </summary>
    public Guid? CategoryId { get; set; }
    
    /// <summary>
    /// Lọc theo trạng thái phê duyệt
    /// </summary>
    public ApprovalStatus? ApprovalStatus { get; set; }
    
    /// <summary>
    /// Lọc theo trạng thái sách
    /// </summary>
    public EntityStatus? Status { get; set; }
    
    /// <summary>
    /// Lọc theo loại sách có phí hay không
    /// </summary>
    public bool? IsPremium { get; set; }
    
    /// <summary>
    /// Lọc theo tags (chứa)
    /// </summary>
    public string? Tags { get; set; }
    
    /// <summary>
    /// Lọc theo sách có chapters hay không
    /// </summary>
    public bool? HasChapters { get; set; }
    
    /// <summary>
    /// Lọc từ ngày xuất bản
    /// </summary>
    public DateTime? PublishedDateFrom { get; set; }
    
    /// <summary>
    /// Lọc đến ngày xuất bản
    /// </summary>
    public DateTime? PublishedDateTo { get; set; }
    
    /// <summary>
    /// Tìm kiếm trong tất cả các trường text (Title, Author, ISBN, Publisher, Tags)
    /// </summary>
    public string? Search { get; set; }
    
    public BookFilterModel() : base()
    {
    }
    
    public BookFilterModel(int pageNumber, int pageSize) : base(pageNumber, pageSize)
    {
    }
} 