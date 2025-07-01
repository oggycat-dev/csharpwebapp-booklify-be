using MediatR;
using Booklify.Application.Common.DTOs.ChapterNote;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.ChapterNote.Commands.UpdateChapterNote;

public record UpdateChapterNoteCommand(Guid NoteId, UpdateChapterNoteRequest Request) : IRequest<Result<ChapterNoteResponse>>;
