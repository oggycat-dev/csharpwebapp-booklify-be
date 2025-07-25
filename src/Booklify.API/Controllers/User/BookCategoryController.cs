using MediatR;
using Microsoft.AspNetCore.Mvc;
using Booklify.API.Configurations;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.DTOs.BookCategory;
using Booklify.Application.Features.BookCategory.Queries.GetBookCategories;
using Booklify.Domain.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace Booklify.API.Controllers.User;

/// <summary>
/// Controller quản lý danh mục sách cho API công khai
/// </summary>
[ApiController]
[Route("api/book-categories")]
[ApiExplorerSettings(GroupName = "v1")]
[Configurations.Tags("User", "User_BookCategory")]
[SwaggerTag("API danh mục sách công khai")]
public class BookCategoryController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public BookCategoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy danh sách danh mục sách với tùy chọn lọc, sắp xếp và phân trang
    /// </summary>
    /// <remarks>
    /// API này hỗ trợ lọc, sắp xếp và phân trang cho danh sách danh mục sách.
    /// 
    /// Các tham số lọc:
    /// - name: Lọc theo tên danh mục (tìm kiếm gần đúng)
    /// - description: Lọc theo mô tả (tìm kiếm gần đúng)
    /// - status: Lọc theo trạng thái (0: Inactive, 1: Active)
    /// 
    /// Các tham số sắp xếp:
    /// - sortBy: Trường dữ liệu dùng để sắp xếp (name, description, status, createdat, bookscount)
    /// - isAscending: Sắp xếp tăng dần (true) hoặc giảm dần (false)
    /// 
    /// Các tham số phân trang:
    /// - pageNumber: Số trang (mặc định: 1)
    /// - pageSize: Số lượng bản ghi trên một trang (mặc định: 10)
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<BookCategoryResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 500)]
    [SwaggerOperation(
        Summary = "Lấy danh sách danh mục sách",
        Description = "Lấy danh sách danh mục sách với tùy chọn lọc, sắp xếp và phân trang. API công khai, không cần authentication.",
        OperationId = "GetBookCategories",
        Tags = new[] { "User", "User_BookCategory" }
    )]
    public async Task<IActionResult> GetBookCategories(
        [FromQuery] string? name,
        [FromQuery] string? description,
        [FromQuery] EntityStatus? status,
        [FromQuery] string? sortBy,
        [FromQuery] bool isAscending = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var filter = new BookCategoryFilterModel(pageNumber, pageSize)
        {
            Name = name,
            Description = description,
            Status = status,
            SortBy = sortBy,
            IsAscending = isAscending
        };
        
        var result = await _mediator.Send(new GetBookCategoriesQuery(filter));
        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }
        return Ok(result);
    }
} 