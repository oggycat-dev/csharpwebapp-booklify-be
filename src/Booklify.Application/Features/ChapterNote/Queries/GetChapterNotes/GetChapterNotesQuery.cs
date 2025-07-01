using MediatR;
using Booklify.Application.Common.DTOs.ChapterNote;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.ChapterNote.Queries.GetChapterNotes;

public class GetChapterNotesQuery : IRequest<Result<PaginatedResult<ChapterNoteListItemResponse>>>
{
    public ChapterNoteFilterModel Filter { get; set; } = new();
}
