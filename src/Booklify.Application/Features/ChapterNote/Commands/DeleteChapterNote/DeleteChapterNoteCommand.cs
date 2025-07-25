using MediatR;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.ChapterNote.Commands.DeleteChapterNote;

public class DeleteChapterNoteCommand : IRequest<Result<bool>>
{
    public Guid NoteId { get; set; }
}
