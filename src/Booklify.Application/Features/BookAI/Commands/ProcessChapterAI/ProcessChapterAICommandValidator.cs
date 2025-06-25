using FluentValidation;

namespace Booklify.Application.Features.BookAI.Commands.ProcessChapterAI;

public class ProcessChapterAICommandValidator : AbstractValidator<ProcessChapterAICommand>
{
    private readonly string[] _validActions = { "summary", "keywords", "translation", "flashcards" };
    
    public ProcessChapterAICommandValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty()
            .WithMessage("Book ID is required");
            
        RuleFor(x => x.ChapterIndex)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Chapter index must be 0 or greater");
            
        RuleFor(x => x.Actions)
            .NotEmpty()
            .WithMessage("At least one action is required");
            
        RuleForEach(x => x.Actions)
            .Must(action => _validActions.Contains(action.ToLower()))
            .WithMessage($"Invalid action. Valid actions are: {string.Join(", ", _validActions)}");
    }
} 