using MediatR;
using Booklify.Application.Common.DTOs.ChapterNote;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.ChapterNote.Queries.GetChapterNoteById;

public class GetChapterNoteByIdQuery : IRequest<Result<ChapterNoteResponse>>
{
    public Guid NoteId { get; set; }
}
