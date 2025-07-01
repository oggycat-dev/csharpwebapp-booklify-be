using MediatR;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.DTOs.ChapterNote;

namespace Booklify.Application.Features.ChapterNote.Commands.CreateChapterNote;

public record CreateChapterNoteCommand(CreateChapterNoteRequest Request) : IRequest<Result<ChapterNoteResponse>>; 