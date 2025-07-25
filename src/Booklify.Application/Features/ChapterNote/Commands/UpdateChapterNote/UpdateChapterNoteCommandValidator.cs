using FluentValidation;
using Booklify.Domain.Enums;

namespace Booklify.Application.Features.ChapterNote.Commands.UpdateChapterNote;

public class UpdateChapterNoteCommandValidator : AbstractValidator<UpdateChapterNoteCommand>
{
    public UpdateChapterNoteCommandValidator()
    {
        RuleFor(x => x.NoteId)
            .NotEmpty().WithMessage("Note ID is required");

        RuleFor(x => x.Request.Content)
            .MaximumLength(1000).WithMessage("Content cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Request.Content));

        RuleFor(x => x.Request.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0")
            .When(x => x.Request.PageNumber.HasValue);

        RuleFor(x => x.Request.Color)
            .MaximumLength(20).WithMessage("Color cannot exceed 20 characters")
            .When(x => !string.IsNullOrEmpty(x.Request.Color));

        RuleFor(x => x.Request.HighlightedText)
            .MaximumLength(500).WithMessage("Highlighted text cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Request.HighlightedText));

        RuleFor(x => x.Request.CfiStart)
            .MaximumLength(200).WithMessage("CFI start cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Request.CfiStart));
        RuleFor(x => x.Request.CfiEnd)
            .MaximumLength(200).WithMessage("CFI end cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Request.CfiEnd));

        RuleFor(x => x.Request.NoteType)
            .IsInEnum().WithMessage("Invalid note type")
            .When(x => x.Request.NoteType.HasValue);
    }
}
