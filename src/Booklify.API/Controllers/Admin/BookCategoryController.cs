using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Booklify.API.Configurations;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.DTOs.BookCategory;
using Booklify.Application.Features.BookCategory.Commands.CreateBookCategory;
using Booklify.Application.Features.BookCategory.Commands.UpdateBookCategory;
using Booklify.Application.Features.BookCategory.Commands.DeleteBookCategory;
using Booklify.Application.Features.BookCategory.Queries.GetBookCategories;
using Booklify.Application.Features.BookCategory.Queries.GetBookCategoryById;
using Booklify.Domain.Enums;
using Swashbuckle.AspNetCore.Annotations;
using Booklify.API.Attributes;

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
    /// Lấy chi tiết danh mục sách theo ID
    /// </summary>
    /// <param name="id">ID của danh mục sách</param>
    /// <returns>Thông tin chi tiết danh mục sách</returns>
    /// <response code="200">Lấy thông tin thành công</response>
    /// <response code="401">Không có quyền truy cập</response>
    /// <response code="403">Không đủ quyền hạn (yêu cầu quyền Admin)</response>
    /// <response code="404">Không tìm thấy danh mục sách</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<BookCategoryResponse>), 200)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 403)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Lấy chi tiết danh mục sách",
        Description = "Lấy thông tin chi tiết của một danh mục sách theo ID",
        OperationId = "Admin_GetBookCategoryById",
        Tags = new[] { "Admin", "Admin_BookCategory" }
    )]
    public async Task<IActionResult> GetBookCategoryById(Guid id)
    {
        var query = new GetBookCategoryByIdQuery(id);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);

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

    /// <summary>
    /// Cập nhật danh mục sách
    /// </summary>
    /// <remarks>
    /// API hỗ trợ cập nhật từng phần (partial update). Chỉ các trường được cung cấp sẽ được cập nhật.
    /// 
    /// Mẫu request:
    /// 
    ///     PATCH /api/cms/book-categories/{id}
    ///     {
    ///        "name": "Tiểu thuyết cập nhật",
    ///        "description": "Mô tả mới",
    ///        "isActive": true
    ///     }
    /// </remarks>
    /// <param name="id">ID của danh mục sách</param>
    /// <param name="request">Thông tin cập nhật</param>
    /// <returns>Thông tin danh mục sách đã cập nhật</returns>
    /// <response code="200">Cập nhật danh mục sách thành công</response>
    /// <response code="400">Dữ liệu không hợp lệ hoặc không có thay đổi</response>
    /// <response code="401">Không có quyền truy cập</response>
    /// <response code="403">Không đủ quyền hạn (yêu cầu quyền Admin)</response>
    /// <response code="404">Không tìm thấy danh mục sách</response>
    [HttpPatch("{id}")]
    [SkipModelValidation]
    [ProducesResponseType(typeof(Result<BookCategoryResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 403)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Cập nhật danh mục sách",
        Description = "API cập nhật danh mục sách dành cho quản trị viên. Hỗ trợ cập nhật từng phần.",
        OperationId = "Admin_UpdateBookCategory",
        Tags = new[] { "Admin", "Admin_BookCategory" }
    )]
    public async Task<IActionResult> UpdateBookCategory(Guid id, [FromBody] UpdateBookCategoryRequest request)
    {
        var command = new UpdateBookCategoryCommand(id, request);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }

    /// <summary>
    /// Xóa danh mục sách
    /// </summary>
    /// <remarks>
    /// API thực hiện xóa mềm (soft delete) bằng cách đặt trạng thái thành Inactive.
    /// 
    /// Lưu ý: Không thể xóa danh mục sách nếu còn chứa sách. Cần di chuyển hoặc xóa tất cả sách trong danh mục trước.
    /// </remarks>
    /// <param name="id">ID của danh mục sách cần xóa</param>
    /// <returns>Kết quả xóa</returns>
    /// <response code="200">Xóa danh mục sách thành công</response>
    /// <response code="400">Không thể xóa danh mục sách chứa sách</response>
    /// <response code="401">Không có quyền truy cập</response>
    /// <response code="403">Không đủ quyền hạn (yêu cầu quyền Admin)</response>
    /// <response code="404">Không tìm thấy danh mục sách</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 403)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Xóa danh mục sách",
        Description = "API xóa danh mục sách dành cho quản trị viên. Thực hiện xóa mềm.",
        OperationId = "Admin_DeleteBookCategory",
        Tags = new[] { "Admin", "Admin_BookCategory" }
    )]
    public async Task<IActionResult> DeleteBookCategory(Guid id)
    {
        var command = new DeleteBookCategoryCommand(id);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }
} 