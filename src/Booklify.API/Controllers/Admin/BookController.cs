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
    /// Form data request (multipart/form-data):
    /// - file: File sách (bắt buộc) - định dạng: PDF, EPUB, DOCX, TXT
    /// - title: Tiêu đề sách (bắt buộc)
    /// - description: Mô tả sách (không bắt buộc)
    /// - author: Tác giả (bắt buộc)
    /// - isbn: Mã ISBN (không bắt buộc)
    /// - publisher: Nhà xuất bản (không bắt buộc)
         /// - category_id: ID danh mục sách (bắt buộc) - định dạng: GUID
     /// - is_premium: Sách có phí hay không (không bắt buộc) - true/false
     /// - tags: Thẻ tag (không bắt buộc) - phân cách bằng dấu phẩy
     /// - published_date: Ngày xuất bản (không bắt buộc) - định dạng: yyyy-MM-dd
    /// 
    /// Giới hạn file:
    /// - Kích thước tối đa: 50MB
    /// - Định dạng được hỗ trợ: .pdf, .epub, .docx, .txt
    /// 
    /// Lưu ý:
    /// - Đối với EPUB: metadata sẽ được extract tự động trong background
    /// - Approval status: Admin sẽ được auto-approve, Staff cần approval
    /// </remarks>
    /// <param name="title">Tiêu đề sách</param>
    /// <param name="description">Mô tả sách</param>
    /// <param name="author">Tác giả</param>
    /// <param name="isbn">Mã ISBN</param>
    /// <param name="publisher">Nhà xuất bản</param>
         /// <param name="categoryId">ID danh mục sách</param>
     /// <param name="isPremium">Sách có phí hay không</param>
     /// <param name="tags">Thẻ tag (phân cách bằng dấu phẩy)</param>
     /// <param name="publishedDate">Ngày xuất bản</param>
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
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateBook(
        [FromForm(Name = "title")] string title,
        [FromForm(Name = "description")] string description = "",
        [FromForm(Name = "author")] string author = "",
        [FromForm(Name = "isbn")] string isbn = "",
        [FromForm(Name = "publisher")] string publisher = "",
        [FromForm(Name = "category_id")] Guid categoryId = default,
        [FromForm(Name = "is_premium")] bool isPremium = false,
        [FromForm(Name = "tags")] string? tags = null,
        [FromForm(Name = "published_date")] DateTime? publishedDate = null)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(title))
        {
            return BadRequest(Result.Failure("Tiêu đề sách là bắt buộc"));
        }

        if (string.IsNullOrWhiteSpace(author))
        {
            return BadRequest(Result.Failure("Tác giả là bắt buộc"));
        }

        if (categoryId == Guid.Empty)
        {
            return BadRequest(Result.Failure("ID danh mục sách là bắt buộc"));
        }

        // Get the file from form
        var file = Request.Form.Files.GetFile("file");
        if (file == null)
        {
            return BadRequest(Result.Failure("File sách là bắt buộc"));
        }

        // Create the request object
        var request = new CreateBookRequest
        {
            Title = title,
            Description = description,
            Author = author,
            ISBN = isbn,
            Publisher = publisher,
            CategoryId = categoryId,
            IsPremium = isPremium,
            Tags = tags,
            PublishedDate = publishedDate,
            File = file
        };

        var command = new CreateBookCommand(request);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);

        return Ok(result);
    }
} 