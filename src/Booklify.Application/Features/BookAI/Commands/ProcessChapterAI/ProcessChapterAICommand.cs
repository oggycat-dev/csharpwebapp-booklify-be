using MediatR;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.DTOs.BookAI;

namespace Booklify.Application.Features.BookAI.Commands.ProcessChapterAI;

public class ProcessChapterAICommand : IRequest<Result<ChapterAIResponse>>
{
    public Guid BookId { get; set; }
    public int ChapterIndex { get; set; }
    public List<string> Actions { get; set; } = new();
    
    public ProcessChapterAICommand(Guid bookId, int chapterIndex, List<string> actions)
    {
        BookId = bookId;
        ChapterIndex = chapterIndex;
        Actions = actions;
    }
} 