using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Booklify.API.Configurations;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.DTOs.BookCategory;
using Booklify.Application.Features.BookCategory.Commands.CreateBookCategory;
using Booklify.Application.Features.BookCategory.Queries.GetBookCategories;
using Booklify.Domain.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace Booklify.API.Controllers.Admin;

/// <summary>
/// Controller quản lý danh mục sách cho ứng dụng quản trị viên
/// </summary>
[ApiController]
[Route("api/cms/book-categories")]
[ApiExplorerSettings(GroupName = "v1")]
[Authorize(Roles = "Admin")]
[Configurations.Tags("Admin", "Admin_BookCategory")]
[SwaggerTag("API quản lý danh mục sách dành cho quản trị viên")]
public class BookCategoryController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public BookCategoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy danh sách danh mục sách với tùy chọn lọc, sắp xếp và phân trang (Admin)
    /// </summary>
    /// <remarks>
    /// API này hỗ trợ lọc, sắp xếp và phân trang cho danh sách danh mục sách dành cho admin.
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
    [HttpGet("list")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResult<BookCategoryResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 403)]
    [ProducesResponseType(typeof(Result), 500)]
    [SwaggerOperation(
        Summary = "Lấy danh sách danh mục sách (Admin)",
        Description = "Lấy danh sách danh mục sách với tùy chọn lọc, sắp xếp và phân trang. Chỉ Admin mới có quyền truy cập.",
        OperationId = "Admin_GetBookCategories",
        Tags = new[] { "Admin", "Admin_BookCategory" }
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
    
    /// <summary>
    /// Tạo danh mục sách mới
    /// </summary>
    /// <remarks>
    /// Mẫu request:
    /// 
    ///     POST /api/cms/book-categories
    ///     {
    ///        "name": "Tiểu thuyết",
    ///        "description": "Danh mục dành cho các tiểu thuyết"
    ///     }
    /// </remarks>
    /// <param name="request">Thông tin danh mục sách mới</param>
    /// <returns>Thông tin danh mục sách đã tạo</returns>
    /// <response code="200">Tạo danh mục sách thành công</response>
    /// <response code="400">Dữ liệu không hợp lệ</response>
    /// <response code="401">Không có quyền truy cập</response>
    /// <response code="403">Không đủ quyền hạn (yêu cầu quyền Admin)</response>
    [HttpPost]
    [ProducesResponseType(typeof(Result<CreatedBookCategoryResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 403)]
    [SwaggerOperation(
        Summary = "Tạo danh mục sách mới",
        Description = "API tạo danh mục sách dành cho quản trị viên",
        OperationId = "Admin_CreateBookCategory",
        Tags = new[] { "Admin", "Admin_BookCategory" }
    )]
    public async Task<IActionResult> CreateBookCategory([FromBody] CreateBookCategoryRequest request)
    {
        var command = new CreateBookCategoryCommand(request);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }
} 