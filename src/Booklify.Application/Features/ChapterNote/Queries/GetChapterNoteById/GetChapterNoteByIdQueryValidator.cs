using FluentValidation;

namespace Booklify.Application.Features.ChapterNote.Queries.GetChapterNoteById;

public class GetChapterNoteByIdQueryValidator : AbstractValidator<GetChapterNoteByIdQuery>
{
    public GetChapterNoteByIdQueryValidator()
    {
        RuleFor(x => x.NoteId)
            .NotEmpty().WithMessage("Note ID is required");
    }
}
