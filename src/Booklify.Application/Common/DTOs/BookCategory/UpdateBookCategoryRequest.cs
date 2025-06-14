using System.ComponentModel.DataAnnotations;

namespace Booklify.Application.Common.DTOs.BookCategory;

public class UpdateBookCategoryRequest
{
    /// <summary>
    /// Tên danh mục sách (tùy chọn)
    /// </summary>
    [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự")]
    public string? Name { get; set; }

    /// <summary>
    /// Mô tả danh mục sách (tùy chọn)
    /// </summary>
    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    public string? Description { get; set; }

    /// <summary>
    /// Trạng thái danh mục (tùy chọn)
    /// </summary>
    public bool? IsActive { get; set; }
} 