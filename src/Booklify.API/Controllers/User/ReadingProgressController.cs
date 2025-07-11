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
    /// API tracking tiến độ đọc sách theo chapter:
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
    /// **Ví dụ use cases:**
    /// ```
    /// // User access chapter mới lần đầu
    /// POST /api/reading-progress/track-reading
    /// {
    ///   "book_id": "123e4567-e89b-12d3-a456-426614174000",
    ///   "chapter_id": "456e7890-e89b-12d3-a456-426614174001",
    ///   "current_cfi": "epubcfi(/6/4[ch01]!/4/2/1:0)",
    ///   "is_completed": false
    /// }
    /// 
    /// // User scroll trong chapter (update position)
    /// POST /api/reading-progress/track-reading
    /// {
    ///   "book_id": "123e4567-e89b-12d3-a456-426614174000", 
    ///   "chapter_id": "456e7890-e89b-12d3-a456-426614174001",
    ///   "current_cfi": "epubcfi(/6/4[ch01]!/4/2/1:256)",
    ///   "is_completed": false
    /// }
    /// 
    /// // User hoàn thành chapter
    /// POST /api/reading-progress/track-reading
    /// {
    ///   "book_id": "123e4567-e89b-12d3-a456-426614174000",
    ///   "chapter_id": "456e7890-e89b-12d3-a456-426614174001", 
    ///   "current_cfi": "epubcfi(/6/4[ch01]!/4/2/1:1024)",
    ///   "is_completed": true  // IMMUTABLE - cannot revert
    /// }
    /// ```
    /// 
    /// **Response trả về:**
    /// - 200: Tracking thành công với updated progress
    /// - 400: Validation error (BookId/ChapterId invalid)
    /// - 404: Book/Chapter không tồn tại hoặc không có quyền
    /// - 401: Chưa authenticate
    /// </summary>
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