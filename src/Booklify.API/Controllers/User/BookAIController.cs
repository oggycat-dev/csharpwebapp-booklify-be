using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Booklify.API.Configurations;
using Booklify.Application.Common.DTOs.BookAI;
using Booklify.Application.Common.Models;
using Booklify.Application.Features.BookAI.Commands.ProcessChapterAI;
using Swashbuckle.AspNetCore.Annotations;

namespace Booklify.API.Controllers.User;

/// <summary>
/// Controller for AI processing of book chapters
/// </summary>
[ApiController]
[Route("api/book-ai")]
[ApiExplorerSettings(GroupName = "v1")]
[Authorize]
[Microsoft.AspNetCore.Http.Tags("User", "User_BookAI")]
[SwaggerTag("API xử lý AI cho chapters của sách")]
public class BookAIController : ControllerBase
{
    private readonly IMediator _mediator;

    public BookAIController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Xử lý AI cho một chương cụ thể
    /// </summary>
    /// <remarks>
    /// API cho phép người dùng chọn các hành động AI để xử lý một chương sách:
    /// 
    /// **Các hành động có thể chọn:**
    /// - `summary`: Tóm tắt nội dung chương bằng tiếng Việt
    /// - `keywords`: Trích xuất từ khóa quan trọng
    /// - `translation`: Dịch toàn bộ chương sang tiếng Việt
    /// - `flashcards`: Tạo flashcard từ vựng từ nội dung chương
    /// 
    /// **Ví dụ request body:**
    /// ```json
    /// {
    ///   "actions": ["summary", "keywords", "flashcards"],
    ///   "content": "This is the chapter content to be processed by AI..."
    /// }
    /// ```
    /// 
    /// **Lưu ý:**
    /// - Có thể chọn một hoặc nhiều hành động
    /// - Cần gửi kèm nội dung chương trong trường `content`
    /// - Kết quả được trả về ngay lập tức (không qua background job)
    /// - Cần có quyền truy cập vào sách
    /// </remarks>
    /// <param name="bookId">ID của sách</param>
    /// <param name="chapterId">ID của chương cần xử lý</param>
    /// <param name="request">Danh sách các hành động AI cần thực hiện và nội dung chương</param>
    /// <returns>Kết quả xử lý AI cho chương</returns>
    /// <response code="200">Xử lý AI thành công</response>
    /// <response code="400">Dữ liệu đầu vào không hợp lệ</response>
    /// <response code="401">Chưa đăng nhập</response>
    /// <response code="404">Không tìm thấy sách hoặc chương</response>
    /// <response code="500">Lỗi server hoặc lỗi từ Gemini API</response>
    [HttpPost("{bookId}/chapters/{chapterId}/process-ai")]
    [ProducesResponseType(typeof(Result<ChapterAIResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 404)]
    [ProducesResponseType(typeof(Result), 500)]
    [SwaggerOperation(
        Summary = "Xử lý AI cho chương sách",
        Description = "Thực hiện các tác vụ AI (tóm tắt, từ khóa, dịch thuật, flashcard) cho một chương cụ thể",
        OperationId = "ProcessChapterAI",
        Tags = new[] { "User", "User_BookAI" }
    )]
    public async Task<IActionResult> ProcessChapterAI(
        [FromRoute] Guid bookId,
        [FromRoute] Guid chapterId,
        [FromBody] ChapterAIRequest request)
    {
        var command = new ProcessChapterAICommand(bookId, chapterId, request.Content, request.Actions);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);

        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách các chương của sách
    /// </summary>
    /// <remarks>
    /// API để lấy danh sách tất cả các chương của một sách, 
    /// giúp người dùng biết được ID chương để sử dụng trong API xử lý AI.
    /// </remarks>
    /// <param name="bookId">ID của sách</param>
    /// <returns>Danh sách các chương với thông tin cơ bản</returns>
    /// <response code="200">Lấy danh sách chương thành công</response>
    /// <response code="401">Chưa đăng nhập</response>
    /// <response code="404">Không tìm thấy sách</response>
    [HttpGet("{bookId}/chapters")]
    [ProducesResponseType(typeof(Result<List<ChapterInfo>>), 200)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Lấy danh sách chương",
        Description = "Lấy danh sách các chương của sách với thông tin cơ bản",
        OperationId = "GetBookChapters",
        Tags = new[] { "User", "User_BookAI" }
    )]
    public async Task<IActionResult> GetBookChapters([FromRoute] Guid bookId)
    {
        // Implement this in a separate query handler if needed
        return Ok(Result<List<ChapterInfo>>.Success(new List<ChapterInfo>()));
    }
} 