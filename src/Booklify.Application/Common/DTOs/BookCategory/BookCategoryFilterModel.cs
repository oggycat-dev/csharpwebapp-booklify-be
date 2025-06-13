using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;

namespace Booklify.Application.Common.DTOs.BookCategory;

/// <summary>
/// Mô hình lọc danh sách danh mục sách
/// </summary>
public class BookCategoryFilterModel : FilterBase
{
    /// <summary>
    /// Lọc theo tên danh mục (chứa)
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Lọc theo mô tả (chứa)
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Lọc theo trạng thái
    /// </summary>
    public EntityStatus? Status { get; set; }

    public BookCategoryFilterModel() : base()
    {
    }
    
    public BookCategoryFilterModel(int pageNumber, int pageSize) : base(pageNumber, pageSize)
    {
    }
} 