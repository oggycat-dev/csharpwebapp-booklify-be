using MediatR;
using Microsoft.AspNetCore.Mvc;
using Booklify.API.Configurations;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Features.Book.Queries.GetBooks;
using Booklify.Application.Features.Book.Queries.GetBookById;
using Booklify.Application.Features.Book.Queries.DownloadBook;
using Booklify.Application.Features.Book.Commands.IncrementBookViews;
using Booklify.Domain.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace Booklify.API.Controllers.User;

/// <summary>
/// Controller quản lý sách cho người dùng
/// </summary>
[ApiController]
[Route("api/books")]
[ApiExplorerSettings(GroupName = "v1")]
[Configurations.Tags("User", "User_Book")]
[SwaggerTag("API quản lý sách dành cho người dùng")]
public class BookController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public BookController(IMediator mediator)
    {
        _mediator = mediator;
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
    /// - categoryId: Lọc theo danh mục
    /// - isPremium: Lọc sách premium (true/false)
    /// - hasChapters: Lọc sách có chapters (true/false)
    /// - publishedDateFrom: Lọc từ ngày xuất bản
    /// - publishedDateTo: Lọc đến ngày xuất bản
    /// - search: Tìm kiếm trong tất cả các trường text (Title, Author, ISBN, Publisher, Tags)
    /// - minRating: Lọc theo rating trung bình tối thiểu (0.0 - 5.0)
    /// - maxRating: Lọc theo rating trung bình tối đa (0.0 - 5.0)
    /// - minTotalRatings: Lọc theo số lượng đánh giá tối thiểu
    /// - minTotalViews: Lọc theo số lượt xem tối thiểu
    /// - maxTotalViews: Lọc theo số lượt xem tối đa
    /// 
    /// Các tham số sắp xếp:
    /// - sortBy: Trường dữ liệu dùng để sắp xếp (title, author, createdat, rating, totalratings, totalviews)
    /// - isAscending: Sắp xếp tăng dần (true) hoặc giảm dần (false)
    /// 
    /// Các tham số phân trang:
    /// - pageNumber: Số trang (mặc định: 1)
    /// - pageSize: Số lượng bản ghi trên một trang (mặc định: 10)
    /// </remarks>
    [HttpGet("list")]
    [ProducesResponseType(typeof(PaginatedResult<BookListItemResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 500)]
    [SwaggerOperation(
        Summary = "Lấy danh sách sách",
        Description = "Lấy danh sách sách với tùy chọn lọc, sắp xếp và phân trang",
        OperationId = "User_GetBooks",
        Tags = new[] { "User", "User_Book" }
    )]
    public async Task<IActionResult> GetBooks(
        [FromQuery] string? title,
        [FromQuery] string? author,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? isPremium,
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
            CategoryId = categoryId,
            IsPremium = isPremium,
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
            IsAscending = isAscending,
            // Fix cứng trạng thái cho trang khách hàng
            Status = EntityStatus.Active,
            ApprovalStatus = ApprovalStatus.Approved
        };
        
        var query = new GetBookListItemsQuery(filter);
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một cuốn sách
    /// </summary>
    /// <remarks>
    /// API này kiểm tra quyền truy cập cho sách premium:
    /// 
    /// - Chỉ hiển thị sách đã được duyệt (Approved) và đang hoạt động (Active)
    /// - Sách thường: Trả về đầy đủ thông tin và chapters
    /// - Sách premium:
    ///   + Guest/User không có subscription: Chỉ 2 chapters đầu tiên
    ///   + User có subscription active: Đầy đủ chapters
    ///   + Admin/Staff: Đầy đủ chapters (không bị giới hạn)
    /// 
    /// Subscription được kiểm tra dựa trên:
    /// - Subscription đang active (IsActive = true)
    /// - Trong thời gian hiệu lực (StartDate <= now <= EndDate)
    /// - Trạng thái subscription = Active
    /// </remarks>
    /// <param name="id">ID của sách</param>
    /// <returns>Thông tin chi tiết của sách với kiểm tra quyền truy cập</returns>
    /// <response code="200">Lấy thông tin sách thành công</response>
    /// <response code="404">Không tìm thấy sách hoặc sách không được phép xem</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<BookResponse>), 200)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Lấy thông tin chi tiết sách với kiểm tra subscription",
        Description = "Lấy thông tin chi tiết của một cuốn sách theo ID với kiểm tra quyền truy cập premium content",
        OperationId = "User_GetBookById",
        Tags = new[] { "User", "User_Book" }
    )]
    public async Task<IActionResult> GetBookById(Guid id)
    {
        var query = new GetBookByIdQuery(id);
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }

    /// <summary>
    /// Tăng lượt xem cho sách
    /// </summary>
    /// <remarks>
    /// API này được gọi khi người dùng bấm vào nút đọc sách trên giao diện chi tiết sách.
    /// Mỗi lần gọi sẽ tăng lượt xem lên 1 đơn vị.
    /// </remarks>
    /// <param name="id">ID của sách</param>
    /// <returns>Kết quả cập nhật lượt xem</returns>
    /// <response code="200">Cập nhật lượt xem thành công</response>
    /// <response code="404">Không tìm thấy sách</response>
    [HttpPost("{id}/increment-views")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Tăng lượt xem cho sách",
        Description = "API tăng lượt xem khi người dùng bấm vào nút đọc sách",
        OperationId = "User_IncrementBookViews",
        Tags = new[] { "User", "User_Book" }
    )]
    public async Task<IActionResult> IncrementBookViews(Guid id)
    {
        var command = new IncrementBookViewsCommand(id);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }

    /// <summary>
    /// Download file sách
    /// </summary>
    /// <remarks>
    /// API download sách với kiểm tra quyền truy cập. Sách premium yêu cầu subscription active.
    /// </remarks>
    /// <param name="id">ID của sách</param>
    /// <returns>File sách để download</returns>
    /// <response code="200">Download file thành công</response>
    /// <response code="403">Sách premium yêu cầu subscription</response>
    /// <response code="404">Không tìm thấy sách hoặc sách không có file</response>
    [HttpGet("{id}/download")]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(typeof(Result), 403)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Download file sách",
        Description = "Download file sách với kiểm tra subscription cho premium content",
        OperationId = "User_DownloadBook",
        Tags = new[] { "User", "User_Book" }
    )]
    public async Task<IActionResult> DownloadBook(Guid id)
    {
        var query = new DownloadBookQuery(id);
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        var downloadData = result.Data;
        
        // Return file với proper headers
        return File(
            downloadData.FileContent,
            downloadData.ContentType,
            downloadData.FileName);
    }
} 