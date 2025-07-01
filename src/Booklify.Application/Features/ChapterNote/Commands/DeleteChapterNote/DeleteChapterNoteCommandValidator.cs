using FluentValidation;

namespace Booklify.Application.Features.ChapterNote.Commands.DeleteChapterNote;

public class DeleteChapterNoteCommandValidator : AbstractValidator<DeleteChapterNoteCommand>
{
    public DeleteChapterNoteCommandValidator()
    {
        RuleFor(x => x.NoteId)
            .NotEmpty().WithMessage("Note ID is required");
    }
}
