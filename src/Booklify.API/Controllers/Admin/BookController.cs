using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Booklify.API.Configurations;
using Booklify.API.Middlewares;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Models;
using Booklify.Application.Features.Book.Commands.CreateBook;
using Booklify.Application.Features.Book.Commands.UpdateBook;
using Booklify.Application.Features.Book.Commands.DeleteBook;
using Booklify.Application.Features.Book.Commands.ManageBookStatus;
using Booklify.Application.Features.Book.Queries.GetBooks;
using Booklify.Application.Features.Book.Queries.DownloadBook;
using Booklify.Domain.Enums;
using Booklify.API.Attributes;
using Swashbuckle.AspNetCore.Annotations;
using Booklify.Application.Features.Book.Queries.GetBookDetail;
using Booklify.Application.Features.Book.Queries.GetBookChapters;
using Booklify.Application.Features.Book.Queries.GetBookStatistics;

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
    /// Lấy thống kê sách
    /// </summary>
    /// <remarks>
    /// API này trả về thống kê tổng quan về sách trong hệ thống:
    /// 
    /// - Số lượng sách theo trạng thái phê duyệt (Pending, Approved, Rejected)
    /// - Số lượng sách theo trạng thái hoạt động (Active, Inactive)
    /// - Số lượng sách premium và free
    /// - Tổng số sách trong hệ thống
    /// 
    /// **Quyền hạn:**
    /// - Admin và Staff đều có quyền truy cập
    /// </remarks>
    /// <returns>Thống kê sách</returns>
    /// <response code="200">Lấy thống kê thành công</response>
    /// <response code="401">Không có quyền truy cập</response>
    /// <response code="403">Không đủ quyền hạn</response>
    /// <response code="500">Lỗi server</response>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(Result<BookStatisticsResponse>), 200)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 403)]
    [ProducesResponseType(typeof(Result), 500)]
    [SwaggerOperation(
        Summary = "Lấy thống kê sách",
        Description = "Lấy thống kê tổng quan về sách trong hệ thống dành cho Admin và Staff.",
        OperationId = "Admin_GetBookStatistics",
        Tags = new[] { "Admin", "Admin_Book" }
    )]
    public async Task<IActionResult> GetBookStatistics()
    {
        var result = await _mediator.Send(new GetBookStatisticsQuery());
        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách sách với tùy chọn lọc, sắp xếp và phân trang
    /// </summary>
    /// <remarks>
    /// API này hỗ trợ lọc, sắp xếp và phân trang cho danh sách sách.
    /// 
    /// Các tham số lọc:
    /// - title: Lọc theo tiêu đề sách (tìm kiếm gần đúng)
    /// - author: Lọc theo tác giả (tìm kiếm gần đúng)
    /// - isbn: Lọc theo ISBN (tìm kiếm gần đúng)
    /// - publisher: Lọc theo nhà xuất bản (tìm kiếm gần đúng)
    /// - categoryId: Lọc theo danh mục sách (GUID)
    /// - approvalStatus: Lọc theo trạng thái phê duyệt (0: Pending, 1: Approved, 2: Rejected)
    /// - status: Lọc theo trạng thái sách (0: Active, 1: Inactive)
    /// - isPremium: Lọc theo loại sách có phí (true/false)
    /// - tags: Lọc theo tags (tìm kiếm gần đúng)
    /// - hasChapters: Lọc theo sách có chapters (true/false)
    /// - publishedDateFrom: Lọc từ ngày xuất bản (yyyy-MM-dd)
    /// - publishedDateTo: Lọc đến ngày xuất bản (yyyy-MM-dd)
    /// - search: Tìm kiếm trong tất cả các trường text (title, author, isbn, publisher, tags)
    /// - minRating: Lọc theo rating trung bình tối thiểu (0.0 - 5.0)
    /// - maxRating: Lọc theo rating trung bình tối đa (0.0 - 5.0)
    /// - minTotalRatings: Lọc theo số lượng đánh giá tối thiểu
    /// - minTotalViews: Lọc theo số lượt xem tối thiểu
    /// - maxTotalViews: Lọc theo số lượt xem tối đa
    /// 
    /// Các tham số sắp xếp:
    /// - sortBy: Trường dữ liệu dùng để sắp xếp (title, author, isbn, publisher, approvalstatus, status, ispremium, pagecount, publisheddate, createdat, rating, totalratings, totalviews)
    /// - isAscending: Sắp xếp tăng dần (true) hoặc giảm dần (false) - mặc định sắp xếp theo ngày tạo mới nhất
    /// 
    /// Các tham số phân trang:
    /// - pageNumber: Số trang (mặc định: 1)
    /// - pageSize: Số lượng bản ghi trên một trang (mặc định: 10)
    /// </remarks>
    [HttpGet("list")]
    [ProducesResponseType(typeof(PaginatedResult<BookListItemResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 403)]
    [ProducesResponseType(typeof(Result), 500)]
    [SwaggerOperation(
        Summary = "Lấy danh sách sách",
        Description = "Lấy danh sách sách với tùy chọn lọc, sắp xếp và phân trang. Admin và Staff đều có quyền truy cập.",
        OperationId = "Admin_GetBooks",
        Tags = new[] { "Admin", "Admin_Book" }
    )]
    public async Task<IActionResult> GetBooks(
        [FromQuery] string? title,
        [FromQuery] string? author,
        [FromQuery] string? isbn,
        [FromQuery] string? publisher,
        [FromQuery] Guid? categoryId,
        [FromQuery] ApprovalStatus? approvalStatus,
        [FromQuery] EntityStatus? status,
        [FromQuery] bool? isPremium,
        [FromQuery] string? tags,
        [FromQuery] bool? hasChapters,
        [FromQuery] DateTime? publishedDateFrom,
        [FromQuery] DateTime? publishedDateTo,
        [FromQuery] string? search,
        [FromQuery] double? minRating,
        [FromQuery] double? maxRating,
        [FromQuery] int? minTotalRatings,
        [FromQuery] int? minTotalViews,
        [FromQuery] int? maxTotalViews,
        [FromQuery] string? sortBy,
        [FromQuery] bool isAscending = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var filter = new BookFilterModel(pageNumber, pageSize)
        {
            Title = title,
            Author = author,
            ISBN = isbn,
            Publisher = publisher,
            CategoryId = categoryId,
            ApprovalStatus = approvalStatus,
            Status = status,
            IsPremium = isPremium,
            Tags = tags,
            HasChapters = hasChapters,
            PublishedDateFrom = publishedDateFrom,
            PublishedDateTo = publishedDateTo,
            Search = search,
            MinRating = minRating,
            MaxRating = maxRating,
            MinTotalRatings = minTotalRatings,
            MinTotalViews = minTotalViews,
            MaxTotalViews = maxTotalViews,
            SortBy = sortBy,
            IsAscending = isAscending
        };
        
        var result = await _mediator.Send(new GetBooksQuery(filter));
        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Tạo sách mới từ file EPUB
    /// </summary>
    /// <remarks>
    /// API này cho phép tạo sách mới từ file EPUB với auto-extraction metadata.
    /// 
    /// **Định dạng hỗ trợ:**
    /// - EPUB (.epub) only
    /// 
    /// **Quy trình xử lý:**
    /// 1. Upload file EPUB
    /// 2. Validate file format và dữ liệu đầu vào
    /// 3. Extract metadata (title, author, publisher, cover) từ EPUB
    /// 4. Tạo record sách với metadata đã extract
    /// 5. Upload file lên storage
    /// 6. Background job xử lý chapters
    /// 
    /// **Quyền hạn:**
    /// - Admin: Tự động được approved
    /// - Staff: Cần phê duyệt từ Admin
    /// 
    /// **Form data request (multipart/form-data):**
    /// - file: File EPUB (bắt buộc)
    /// - category_id: ID danh mục sách (bắt buộc) - định dạng: GUID
    /// - is_premium: Sách có phí hay không (không bắt buộc) - true/false
    /// - tags: Thẻ tag (không bắt buộc) - phân cách bằng dấu phẩy
    /// 
    /// **Giới hạn file:**
    /// - Kích thước tối đa: 50MB
    /// - Định dạng: .epub only
    /// 
    /// **Lưu ý:**
    /// - Metadata (title, author, publisher, etc.) sẽ được extract tự động từ EPUB
    /// - Để chỉnh sửa metadata sau khi tạo, sử dụng API Update Book
    /// - Background job chỉ xử lý chapters, không xử lý metadata
    /// - Cover image sẽ được extract và upload tự động
    /// </remarks>
    /// <param name="categoryId">ID danh mục sách (bắt buộc)</param>
    /// <param name="isPremium">Sách có phí hay không</param>
    /// <param name="tags">Thẻ tag (phân cách bằng dấu phẩy)</param>
    /// <param name="isbn">Mã ISBN (tùy chọn)</param>
    /// <param name="file">File EPUB (bắt buộc)</param>
    /// <returns>Thông tin sách đã tạo với metadata đã extract</returns>
    /// <response code="200">Tạo sách thành công</response>
    /// <response code="400">Dữ liệu không hợp lệ hoặc file không phải EPUB</response>
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
        Summary = "Tạo sách mới từ EPUB",
        Description = "API tạo sách mới từ file EPUB với auto-extraction metadata.",
        OperationId = "Admin_CreateBook",
        Tags = new[] { "Admin", "Admin_Book" }
    )]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateBook(
        [FromForm(Name = "category_id")] Guid categoryId,
        [FromForm(Name = "is_premium")] bool isPremium = false,
        [FromForm(Name = "tags")] string? tags = null,
        [FromForm(Name = "isbn")] string? isbn = null)
    {
        var file = Request.Form.Files.GetFile("file");
        
        if (file == null)
        {
            return BadRequest(Result.Failure("File EPUB là bắt buộc"));
        }

        // Create the request object
        var request = new CreateBookRequest
        {
            CategoryId = categoryId,
            IsPremium = isPremium,
            Tags = tags,
            Isbn = isbn,
            File = file
        };

        var command = new CreateBookCommand(request);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);

        return Ok(result);
    }

    /// <summary>
    /// Lấy thông tin chi tiết sách theo ID
    /// </summary>
    /// <param name="id">ID của sách</param>
    /// <returns>Thông tin chi tiết sách</returns>
    /// <response code="200">Lấy thông tin sách thành công</response>
    /// <response code="401">Không có quyền truy cập</response>
    /// <response code="403">Không đủ quyền hạn</response>
    /// <response code="404">Không tìm thấy sách</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<BookResponse>), 200)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 403)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Lấy thông tin chi tiết sách",
        Description = "Lấy thông tin chi tiết sách theo ID. Admin và Staff đều có quyền truy cập.",
        OperationId = "Admin_GetBookById",
        Tags = new[] { "Admin", "Admin_Book" }
    )]
    public async Task<IActionResult> GetBookById([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new GetBookDetailQuery(id));
        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật thông tin sách (PATCH - Partial Update)
    /// </summary>
    /// <remarks>
    /// API này hỗ trợ cập nhật một phần thông tin sách. Chỉ các trường được gửi lên mới được cập nhật.
    /// 
    /// Mẫu yêu cầu (multipart/form-data):
    /// - title: Tiêu đề sách (không bắt buộc)
    /// - description: Mô tả sách (không bắt buộc)
    /// - author: Tác giả (không bắt buộc)
    /// - isbn: Mã ISBN (không bắt buộc)
    /// - publisher: Nhà xuất bản (không bắt buộc)
    /// - category_id: ID danh mục sách (không bắt buộc)
    /// - status: Trạng thái sách (0: Active, 1: Inactive) (không bắt buộc)
    /// - is_premium: Sách có phí hay không (true/false) (không bắt buộc)
    /// - tags: Thẻ tag (không bắt buộc)
    /// - published_date: Ngày xuất bản (không bắt buộc, định dạng: yyyy-MM-dd)
    /// - file: File sách mới (tùy chọn - để trống nếu không muốn thay đổi file)
    ///     
    /// Lưu ý: 
    /// - Chỉ cần gửi các trường cần cập nhật, các trường không gửi sẽ giữ nguyên giá trị cũ
    /// - Nếu upload file mới, file cũ sẽ được xóa trong background.
    /// - Đối với EPUB: metadata sẽ được extract tự động trong background.
    /// - Trạng thái phê duyệt chỉ Admin mới có quyền thay đổi qua API riêng.
    /// </remarks>
    /// <param name="id">ID của sách cần cập nhật</param>
    /// <param name="title">Tiêu đề sách (không bắt buộc)</param>
    /// <param name="description">Mô tả sách (không bắt buộc)</param>
    /// <param name="author">Tác giả (không bắt buộc)</param>
    /// <param name="isbn">Mã ISBN (không bắt buộc)</param>
    /// <param name="publisher">Nhà xuất bản (không bắt buộc)</param>
    /// <param name="categoryId">ID danh mục sách (không bắt buộc)</param>
    /// <param name="tags">Thẻ tag (không bắt buộc)</param>
    /// <param name="publishedDate">Ngày xuất bản (không bắt buộc)</param>
    /// <returns>Thông tin sách đã cập nhật</returns>
    /// <response code="200">Cập nhật thành công</response>
    /// <response code="400">Dữ liệu không hợp lệ hoặc không có thay đổi</response>
    /// <response code="401">Không có quyền truy cập</response>
    /// <response code="403">Không đủ quyền hạn</response>
    /// <response code="404">Không tìm thấy sách</response>
    [HttpPatch("{id}")]
    [SkipModelValidation] // Skip model validation for partial updates
    [ProducesResponseType(typeof(Result<BookResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 403)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Cập nhật thông tin sách (Partial Update)",
        Description = "API cập nhật một phần thông tin sách. Model validation sẽ bị skip, chỉ validate các trường được gửi lên.",
        OperationId = "Admin_UpdateBook",
        Tags = new[] { "Admin", "Admin_Book" }
    )]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateBook(
        [FromRoute] Guid id,
        [FromForm(Name = "title")] string? title = null,
        [FromForm(Name = "description")] string? description = null,
        [FromForm(Name = "author")] string? author = null,
        [FromForm(Name = "isbn")] string? isbn = null,
        [FromForm(Name = "publisher")] string? publisher = null,
        [FromForm(Name = "category_id")] Guid? categoryId = null,
        [FromForm(Name = "tags")] string? tags = null,
        [FromForm(Name = "published_date")] DateTime? publishedDate = null)
    {
        var request = new UpdateBookRequest
        {
            Title = title,
            Description = description,
            Author = author,
            ISBN = isbn,
            Publisher = publisher,
            CategoryId = categoryId,
            Tags = tags,
            PublishedDate = publishedDate,
        };
        
        // Get the file directly from the form if exists
        var file = Request.Form.Files.GetFile("file");
        
        if (file != null)
        {
            request.File = file;
        }
        
        var command = new UpdateBookCommand(id, request);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }

    /// <summary>
    /// Xóa sách (chỉ Admin)
    /// </summary>
    /// <remarks>
    /// API này cho phép xóa sách (soft delete). Chỉ Admin mới có quyền xóa sách.
    /// 
    /// **Quy trình xóa:**
    /// 1. Kiểm tra quyền Admin
    /// 2. Kiểm tra sách có tồn tại không
    /// 3. Thực hiện soft delete sách
    /// 4. Background jobs sẽ xóa:
    ///    - Chapters của sách
    ///    - File và cover image
    ///    - Các dữ liệu liên quan
    /// 
    /// **Quyền hạn:**
    /// - Chỉ Admin mới có quyền xóa sách
    /// - Staff không thể xóa sách
    /// 
    /// **Lưu ý:**
    /// - Đây là soft delete, dữ liệu không bị xóa hoàn toàn
    /// - Background jobs sẽ dọn dẹp file và chapters
    /// - Không thể khôi phục sau khi xóa (cần Admin can thiệp database)
    /// </remarks>
    /// <param name="id">ID của sách cần xóa</param>
    /// <returns>Kết quả xóa sách</returns>
    /// <response code="200">Xóa sách thành công</response>
    /// <response code="400">Dữ liệu không hợp lệ</response>
    /// <response code="401">Không có quyền truy cập</response>
    /// <response code="403">Không đủ quyền hạn (yêu cầu quyền Admin)</response>
    /// <response code="404">Không tìm thấy sách</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")] // Chỉ Admin mới được xóa sách
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 403)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Xóa sách (chỉ Admin)",
        Description = "API xóa sách với quyền Admin. Sẽ thực hiện soft delete và background cleanup.",
        OperationId = "Admin_DeleteBook",
        Tags = new[] { "Admin", "Admin_Book" }
    )]
    public async Task<IActionResult> DeleteBook([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new DeleteBookCommand(id));
        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Quản lý trạng thái sách (Phê duyệt và Premium) - Chỉ dành cho Admin
    /// </summary>
    /// <remarks>
    /// API này cho phép Admin quản lý trạng thái phê duyệt và trạng thái premium của sách.
    /// 
    /// Mẫu request (chỉ cần gửi các trường muốn cập nhật):
    /// 
    ///     PUT /api/cms/books/{id}/manage-status
    ///     {
    ///        "approval_status": 1,
    ///        "approval_note": "Sách đã được phê duyệt",
    ///        "status": 1,
    ///        "is_premium": true
    ///     }
    ///     
    /// Các trường có thể cập nhật:
    /// - approval_status: Trạng thái phê duyệt (0: Pending, 1: Approved, 2: Rejected)
    /// - status: Trạng thái sách (0: Active, 1: Inactive)
    /// - approval_note: Ghi chú phê duyệt (bắt buộc khi từ chối - status = 2)
    /// - is_premium: Sách có phí hay không (true/false)
    ///     
    /// Lưu ý: 
    /// - Chỉ Admin mới có quyền sử dụng API này
    /// - Phải cung cấp ít nhất một trường để cập nhật
    /// - Ghi chú phê duyệt bắt buộc khi từ chối sách
    /// - Tất cả các thay đổi sẽ được ghi log để audit
    /// </remarks>
    /// <param name="id">ID của sách cần quản lý trạng thái</param>
    /// <param name="request">Thông tin trạng thái cần cập nhật</param>
    /// <returns>Thông tin sách đã cập nhật</returns>
    /// <response code="200">Cập nhật trạng thái thành công</response>
    /// <response code="400">Dữ liệu không hợp lệ hoặc thiếu ghi chú khi từ chối</response>
    /// <response code="401">Không có quyền truy cập</response>
    /// <response code="403">Không đủ quyền hạn (chỉ Admin mới được phép)</response>
    /// <response code="404">Không tìm thấy sách</response>
    [HttpPut("{id}/manage-status")]
    [Authorize(Roles = "Admin")] // Chỉ Admin mới có quyền
    [ProducesResponseType(typeof(Result<BookResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 403)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Quản lý trạng thái sách (Phê duyệt và Premium)",
        Description = "API quản lý trạng thái phê duyệt và premium của sách. Chỉ dành cho Admin.",
        OperationId = "Admin_ManageBookStatus",
        Tags = new[] { "Admin", "Admin_Book" }
    )]
    public async Task<IActionResult> ManageBookStatus([FromRoute] Guid id, [FromBody] ManageBookStatusRequest request)
    {
        var command = new ManageBookStatusCommand(id, request);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }

    /// <summary>
    /// Download file sách
    /// </summary>
    /// <remarks>
    /// API download file sách cho Admin/Staff. Không bị giới hạn bởi subscription hoặc trạng thái sách.
    /// </remarks>
    /// <param name="id">ID của sách</param>
    /// <returns>File sách để download</returns>
    /// <response code="200">Download file thành công</response>
    /// <response code="401">Không có quyền truy cập</response>
    /// <response code="403">Không đủ quyền hạn</response>
    /// <response code="404">Không tìm thấy sách hoặc sách không có file</response>
    [HttpGet("{id}/download")]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 403)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Download file sách",
        Description = "Download file sách cho Admin/Staff - không giới hạn subscription",
        OperationId = "Admin_DownloadBook",
        Tags = new[] { "Admin", "Admin_Book" }
    )]
    public async Task<IActionResult> DownloadBook(Guid id)
    {
        var query = new DownloadBookQuery(id);
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        var downloadData = result.Data;
        
        if (downloadData?.FileContent == null)
            return NotFound(Result.Failure("Không tìm thấy file sách"));
        
        // Return file với proper headers
        return File(
            downloadData.FileContent,
            downloadData.ContentType,
            downloadData.FileName);
    }

    /// <summary>
    /// Lấy danh sách chapters của sách (dành cho admin)
    /// </summary>
    /// <remarks>
    /// API này trả về danh sách chapters của sách dành cho admin:
    /// 
    /// - Admin có thể xem tất cả chapters của mọi sách (kể cả chưa được duyệt)
    /// - Không có giới hạn về subscription như user
    /// - Trả về đầy đủ thông tin chapters để admin có thể quản lý
    /// </remarks>
    /// <param name="id">ID của sách</param>
    /// <returns>Danh sách chapters của sách</returns>
    /// <response code="200">Lấy danh sách chapters thành công</response>
    /// <response code="401">Không có quyền truy cập</response>
    /// <response code="403">Không đủ quyền hạn</response>
    /// <response code="404">Không tìm thấy sách</response>
    [HttpGet("{id}/chapters")]
    [ProducesResponseType(typeof(Result<List<ChapterResponse>>), 200)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 403)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Lấy danh sách chapters của sách (dành cho admin)",
        Description = "Lấy danh sách chapters của sách với quyền admin (không có giới hạn subscription)",
        OperationId = "Admin_GetBookChapters",
        Tags = new[] { "Admin", "Admin_Book" }
    )]
    public async Task<IActionResult> GetBookChapters([FromRoute] Guid id)
    {
        var query = new GetBookChaptersQuery(id);
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }
}