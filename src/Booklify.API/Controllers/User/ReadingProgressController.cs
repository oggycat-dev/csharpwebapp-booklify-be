using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Booklify.API.Configurations;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.DTOs.ReadingProgress;
using Swashbuckle.AspNetCore.Annotations;
using Booklify.Application.Features.ReadingProgress.Commands.StartReading;
using Booklify.Application.Features.ReadingProgress.Queries.GetReadingProgress;

namespace Booklify.API.Controllers.User;

/// <summary>
/// Controller quản lý tiến độ đọc sách cho người dùng
/// </summary>
[ApiController]
[Route("api/reading-progress")]
[ApiExplorerSettings(GroupName = "v1")]
[Authorize]
[Configurations.Tags("User", "User_ReadingProgress")]
[SwaggerTag("API quản lý tiến độ đọc sách dành cho người dùng")]
public class ReadingProgressController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public ReadingProgressController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy tiến độ đọc hiện tại của người dùng cho một cuốn sách
    /// </summary>
    /// <param name="bookId">ID của cuốn sách cần lấy tiến độ đọc</param>
    /// <remarks>
    /// API này trả về thông tin tiến độ đọc hiện tại của người dùng cho một cuốn sách cụ thể.
    /// Yêu cầu gửi kèm token xác thực trong header Authorization.
    /// </remarks>
    /// <returns>Thông tin tiến độ đọc của người dùng</returns>
    /// <response code="200">Lấy tiến độ đọc thành công</response>
    /// <response code="401">Không có quyền truy cập hoặc token không hợp lệ</response>
    /// <response code="404">Không tìm thấy sách hoặc tiến độ đọc</response>
    [HttpGet("{bookId}")]
    [ProducesResponseType(typeof(Result<ReadingProgressResponse>), 200)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Lấy tiến độ đọc hiện tại",
        Description = "Lấy thông tin tiến độ đọc của người dùng cho một cuốn sách",
        OperationId = "User_GetReadingProgress",
        Tags = new[] { "User", "User_ReadingProgress" }
    )]
    public async Task<IActionResult> GetReadingProgress(Guid bookId)
    {
        var query = new GetReadingProgressQuery(bookId);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);

        return Ok(result);
    }

    /// <summary>
    /// Track tiến độ đọc sách theo chapter
    /// </summary>
    /// <param name="request">Thông tin tracking session</param>
    /// <remarks>
    /// API tracking tiến độ đọc sách theo chapter với các chức năng chính:
    /// 
    /// **Chức năng chính:**
    /// - Track chapter access (user đang đọc chapter nào)
    /// - Update vị trí đọc hiện tại bằng CFI position
    /// - Mark chapter completion (immutable - không thể revert)
    /// - Auto-calculate overall progress percentage
    /// 
    /// **Khi nào sử dụng:**
    /// - Khi user chuyển sang đọc chapter mới
    /// - Khi user scroll/navigate trong chapter (update position)
    /// - Khi user hoàn thành chapter (mark completed)
    /// - Định kỳ để save vị trí đọc (auto-save)
    /// 
    /// **Business Rules:**
    /// - Chapter completion is IMMUTABLE: false → true (OK), true → false (IGNORED)
    /// - Overall progress = COUNT(completed chapters) / total chapters * 100
    /// - Frontend controls completion criteria (scroll %, time, user action)
    /// 
    /// **Workflow thông thường:**
    /// 1. User mở chapter → `POST /track-reading` (access tracking)
    /// 2. User đọc → `POST /track-reading` (position updates)
    /// 3. User hoàn thành → `POST /track-reading` với `is_completed: true`
    /// 4. User chuyển chapter → `POST /track-reading` (new chapter access)
    /// 
    /// Mẫu request (JSON):
    /// 
    ///     POST /api/reading-progress/track-reading
    ///     Content-Type: application/json
    ///     {
    ///       "book_id": "123e4567-e89b-12d3-a456-426614174000",
    ///       "chapter_id": "456e7890-e89b-12d3-a456-426614174001",
    ///       "current_cfi": "epubcfi(/6/4[ch01]!/4/2/1:0)",
    ///       "is_completed": false
    ///     }
    /// 
    /// **Các trường trong request:**
    /// - book_id: ID của cuốn sách (bắt buộc)
    /// - chapter_id: ID của chapter đang đọc (bắt buộc)
    /// - current_cfi: Vị trí hiện tại trong chapter (CFI format)
    /// - is_completed: Đánh dấu hoàn thành chapter (immutable)
    /// 
    /// **Lưu ý:**
    /// - Chapter completion là IMMUTABLE: một khi đã mark completed thì không thể revert
    /// - CFI position được dùng để bookmark vị trí đọc chính xác
    /// - API này cần authentication token
    /// </remarks>
    /// <returns>Thông tin session tracking và progress được cập nhật</returns>
    /// <response code="200">Tracking thành công với updated progress</response>
    /// <response code="400">Dữ liệu không hợp lệ (BookId/ChapterId invalid)</response>
    /// <response code="401">Chưa authenticate</response>
    /// <response code="404">Book/Chapter không tồn tại hoặc không có quyền</response>
    [HttpPost("track-reading")]
    [ProducesResponseType(typeof(Result<TrackingSessionResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 404)]
    [ProducesResponseType(typeof(Result), 401)]
    [SwaggerOperation(
        Summary = "Track tiến độ đọc sách theo chapter",
        Description = "Track chapter access, update vị trí đọc và mark completion",
        OperationId = "User_TrackReadingProgress",
        Tags = new[] { "User", "User_ReadingProgress" }
    )]
    public async Task<IActionResult> TrackReadingProgress([FromBody] TrackingReadingSessionRequest request)
    {
        var command = new TrackingReadingSessionCommand(request);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);

        return Ok(result);
    }

} 