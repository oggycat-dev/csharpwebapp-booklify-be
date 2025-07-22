using MediatR;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.DTOs.BookAI;

namespace Booklify.Application.Features.BookAI.Commands.ProcessChapterAI;

public class ProcessChapterAICommand : IRequest<Result<ChapterAIResponse>>
{
    public Guid BookId { get; set; }
    public Guid ChapterId { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<string> Actions { get; set; } = new();
    
    public ProcessChapterAICommand(Guid bookId, Guid chapterId, string content, List<string> actions)
    {
        BookId = bookId;
        ChapterId = chapterId;
        Content = content;
        Actions = actions;
    }
} 