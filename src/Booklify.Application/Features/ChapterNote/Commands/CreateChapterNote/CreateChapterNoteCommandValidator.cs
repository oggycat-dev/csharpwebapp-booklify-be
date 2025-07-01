using FluentValidation;
using Booklify.Domain.Enums;

namespace Booklify.Application.Features.ChapterNote.Commands.CreateChapterNote;

public class CreateChapterNoteCommandValidator : AbstractValidator<CreateChapterNoteCommand>
{
    public CreateChapterNoteCommandValidator()
    {
        RuleFor(x => x.Request.PageNumber)
            .GreaterThan(0).WithMessage("Số trang phải lớn hơn 0");

        RuleFor(x => x.Request.ChapterId)
            .NotEmpty().WithMessage("ID chapter không được để trống");

        RuleFor(x => x.Request.Color)
            .MaximumLength(20).WithMessage("Màu sắc không được vượt quá 20 ký tự")
            .When(x => !string.IsNullOrEmpty(x.Request.Color));

        RuleFor(x => x.Request.HighlightedText)
            .MaximumLength(500).WithMessage("Highlighted text cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Request.HighlightedText));

        RuleFor(x => x.Request.NoteType)
            .IsInEnum().WithMessage("Invalid note type");

        // Conditional validation based on note type
        RuleFor(x => x.Request.Content)
            .NotEmpty().WithMessage("Content is required for Text Note type")
            .MaximumLength(1000).WithMessage("Content cannot exceed 1000 characters")
            .When(x => x.Request.NoteType == ChapterNoteType.TextNote);

        RuleFor(x => x.Request.HighlightedText)
            .NotEmpty().WithMessage("Highlighted text is required for Highlight type")
            .When(x => x.Request.NoteType == ChapterNoteType.Highlight);
    }
} 