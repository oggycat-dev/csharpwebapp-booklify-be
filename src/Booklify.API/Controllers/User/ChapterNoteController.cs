using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Booklify.API.Attributes;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.DTOs.ChapterNote;
using Booklify.Application.Features.ChapterNote.Commands.CreateChapterNote;
using Booklify.Application.Features.ChapterNote.Commands.UpdateChapterNote;
using Booklify.Application.Features.ChapterNote.Commands.DeleteChapterNote;
using Booklify.Application.Features.ChapterNote.Queries.GetChapterNotes;
using Booklify.Application.Features.ChapterNote.Queries.GetChapterNoteById;
using Booklify.Domain.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace Booklify.API.Controllers.User;

/// <summary>
/// Controller for managing chapter notes for users
/// </summary>
[ApiController]
[Route("api/chapter-notes")]
[Authorize(Roles = "User")]
[ApiExplorerSettings(GroupName = "v1")]
[SwaggerTag("API for managing chapter notes for users")]
public class ChapterNoteController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChapterNoteController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new chapter note
    /// </summary>
    /// <param name="request">Chapter note information</param>
    /// <returns>Created chapter note information</returns>
    /// <remarks>
    /// This API supports 2 types of notes:
    /// 
    /// **TextNote (1)**: User-written text notes
    /// - Required: content, page_number, chapter_id
    /// - Optional: cfi, color
    /// 
    /// **Highlight (2)**: Highlighted text notes
    /// - Required: highlighted_text, page_number, chapter_id  
    /// - Optional: content, cfi, color
    /// 
    /// CFI (Canonical Fragment Identifier) is optional for both types to precisely locate content in EPUB.
    /// </remarks>
    /// <response code="200">Chapter note created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost]
    [ProducesResponseType(typeof(Result<ChapterNoteResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 401)]
    [SwaggerOperation(
        Summary = "Create a new chapter note",
        Description = "Create a new chapter note with support for text notes and highlights",
        OperationId = "User_CreateChapterNote",
        Tags = new[] { "User", "ChapterNote" }
    )]
    public async Task<IActionResult> CreateChapterNote([FromBody] CreateChapterNoteRequest request)
    {
        var command = new CreateChapterNoteCommand(request);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);

        return Ok(result);
    }

    /// <summary>
    /// Get list of chapter notes with flexible filtering, sorting and pagination
    /// </summary>
    /// <remarks>
    /// This API supports flexible filtering, sorting and pagination for chapter notes.
    /// 
    /// Filter parameters:
    /// - content: Filter by note content (partial match)
    /// - highlightedText: Filter by highlighted text (partial match)
    /// - search: Global search across content and highlighted text
    /// - chapterId: Filter by specific chapter
    /// - bookId: Filter by specific book
    /// - noteType: Filter by note type (1=TextNote, 2=Highlight)
    /// - color: Filter by note color
    /// - pageNumber_Min/Max: Filter by page number range
    /// - specificPageNumber: Filter by exact page number
    /// 
    /// Sorting parameters:
    /// - sortBy: Sort field (content, page_number, created_at, modified_at, note_type)
    /// - isAscending: Sort direction (true=ascending, false=descending)
    /// 
    /// Pagination parameters:
    /// - pageNumber: Page number (default: 1)
    /// - pageSize: Items per page (default: 10)
    /// </remarks>
    [HttpGet("list")]
    [ProducesResponseType(typeof(Result<PaginatedResult<ChapterNoteListItemResponse>>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 401)]
    [SwaggerOperation(
        Summary = "Get list of chapter notes",
        Description = "Get list of chapter notes with flexible filtering, sorting and pagination",
        OperationId = "User_GetChapterNoteList",
        Tags = new[] { "User", "ChapterNote" }
    )]
    public async Task<IActionResult> GetChapterNoteList(
        [FromQuery] string? content,
        [FromQuery] string? highlightedText,
        [FromQuery] string? search,
        [FromQuery] Guid? chapterId,
        [FromQuery] Guid? bookId,
        [FromQuery] ChapterNoteType? noteType,
        [FromQuery] string? color,
        [FromQuery] int? pageNumber_Min,
        [FromQuery] int? pageNumber_Max,
        [FromQuery] int? specificPageNumber,
        [FromQuery] string? sortBy,
        [FromQuery] bool isAscending = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var filter = new ChapterNoteFilterModel(pageNumber, pageSize)
        {
            Content = content,
            HighlightedText = highlightedText,
            Search = search,
            ChapterId = chapterId,
            BookId = bookId,
            NoteType = noteType,
            Color = color,
            PageNumber_Min = pageNumber_Min,
            PageNumber_Max = pageNumber_Max,
            SpecificPageNumber = specificPageNumber,
            SortBy = sortBy,
            IsAscending = isAscending
        };

        var query = new GetChapterNotesQuery { Filter = filter };
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);

        return Ok(result);
    }

    /// <summary>
    /// Get chapter note details by ID
    /// </summary>
    /// <param name="id">Chapter note ID</param>
    /// <returns>Chapter note details</returns>
    /// <response code="200">Chapter note details retrieved successfully</response>
    /// <response code="404">Chapter note not found</response>
    /// <response code="403">Access denied</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<ChapterNoteResponse>), 200)]
    [ProducesResponseType(typeof(Result), 404)]
    [ProducesResponseType(typeof(Result), 403)]
    [SwaggerOperation(
        Summary = "Get chapter note details",
        Description = "Get detailed information of a chapter note by ID",
        OperationId = "User_GetChapterNoteById",
        Tags = new[] { "User", "ChapterNote" }
    )]
    public async Task<IActionResult> GetChapterNoteById(Guid id)
    {
        var query = new GetChapterNoteByIdQuery { NoteId = id };
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);

        return Ok(result);
    }

    /// <summary>
    /// Update chapter note
    /// </summary>
    /// <param name="id">Chapter note ID</param>
    /// <param name="request">Update information</param>
    /// <returns>Updated chapter note information</returns>
    /// <response code="200">Chapter note updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Chapter note not found</response>
    /// <response code="403">Access denied</response>
    [HttpPatch("{id}")]
    [SkipModelValidation] // Skip model validation for update operations
    [ProducesResponseType(typeof(Result<ChapterNoteResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 404)]
    [ProducesResponseType(typeof(Result), 403)]
    [SwaggerOperation(
        Summary = "Update chapter note",
        Description = "Update chapter note information. Only provided fields will be updated.",
        OperationId = "User_UpdateChapterNote",
        Tags = new[] { "User", "ChapterNote" }
    )]
    public async Task<IActionResult> UpdateChapterNote(Guid id, [FromBody] UpdateChapterNoteRequest request)
    {
        var command = new UpdateChapterNoteCommand(id, request);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);

        return Ok(result);
    }

    /// <summary>
    /// Delete chapter note
    /// </summary>
    /// <param name="id">Chapter note ID</param>
    /// <returns>Deletion result</returns>
    /// <response code="200">Chapter note deleted successfully</response>
    /// <response code="404">Chapter note not found</response>
    /// <response code="403">Access denied</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(Result<bool>), 200)]
    [ProducesResponseType(typeof(Result), 404)]
    [ProducesResponseType(typeof(Result), 403)]
    [SwaggerOperation(
        Summary = "Delete chapter note",
        Description = "Delete a chapter note by ID",
        OperationId = "User_DeleteChapterNote",
        Tags = new[] { "User", "ChapterNote" }
    )]
    public async Task<IActionResult> DeleteChapterNote(Guid id)
    {
        var command = new DeleteChapterNoteCommand { NoteId = id };
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);

        return Ok(result);
    }
}
