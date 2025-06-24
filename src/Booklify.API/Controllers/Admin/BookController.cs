using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Booklify.API.Configurations;
using Booklify.API.Middlewares;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Models;
using Booklify.Application.Features.Book.Commands.CreateBook;
using Swashbuckle.AspNetCore.Annotations;

namespace Booklify.API.Controllers.Admin;

/// <summary>
/// Controller quản lý sách cho ứng dụng quản trị viên
/// </summary>
[ApiController]
[Route("api/cms/books")]
[ApiExplorerSettings(GroupName = "v1")]
[Authorize(Roles = "Admin,Staff")]
[Configurations.Tags("Admin", "Admin_Book")]
[SwaggerTag("API quản lý sách dành cho quản trị viên")]
public class BookController : ControllerBase
{
    private readonly IMediator _mediator;

    public BookController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Tạo sách mới
    /// </summary>
    /// <remarks>
    /// API tạo sách mới với upload file.
    /// Hỗ trợ các định dạng file: PDF, EPUB, DOCX, TXT.
    /// 
    /// Đối với file EPUB, hệ thống sẽ tự động:
    /// - Trích xuất metadata (tác giả, tiêu đề, nhà xuất bản, ảnh bìa)
    /// - Tạo chapters tự động dựa trên table of contents
    /// - Upload ảnh bìa lên storage
    /// 
    /// Form data request:
    /// - File: File sách (bắt buộc)
    /// - Title: Tiêu đề sách (bắt buộc)
    /// - Description: Mô tả sách
    /// - Author: Tác giả (bắt buộc)
    /// - ISBN: Mã ISBN
    /// - Publisher: Nhà xuất bản
    /// - CategoryId: ID danh mục sách (bắt buộc)
    /// - IsPremium: Sách có phí hay không
    /// - Tags: Thẻ tag (phân cách bằng dấu phẩy)
    /// - PublishedDate: Ngày xuất bản (yyyy-MM-dd)
    /// </remarks>
    /// <param name="request">Thông tin sách mới</param>
    /// <returns>Thông tin sách đã tạo</returns>
    /// <response code="200">Tạo sách thành công</response>
    /// <response code="400">Dữ liệu không hợp lệ hoặc file không được hỗ trợ</response>
    /// <response code="401">Không có quyền truy cập</response>
    /// <response code="403">Không đủ quyền hạn (yêu cầu quyền Admin hoặc Staff)</response>
    /// <response code="404">Danh mục sách không tồn tại</response>
    [HttpPost]
    [ProducesResponseType(typeof(Result<BookResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 403)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Tạo sách mới",
        Description = "API tạo sách mới với upload file. Hỗ trợ auto-extract cho EPUB.",
        OperationId = "Admin_CreateBook",
        Tags = new[] { "Admin", "Admin_Book" }
    )]
    public async Task<IActionResult> CreateBook([FromForm] CreateBookRequest request)
    {
        var command = new CreateBookCommand(request);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);

        return Ok(result);
    }
} 