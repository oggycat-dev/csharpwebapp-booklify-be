using FluentValidation;

namespace Booklify.Application.Features.Book.Commands.ResubmitBook;

public class ResubmitBookCommandValidator : AbstractValidator<ResubmitBookCommand>
{
    public ResubmitBookCommandValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty()
            .WithMessage("Book ID là bắt buộc");

        // ResubmitNote is optional, but if provided, should have reasonable length
        RuleFor(x => x.Request.ResubmitNote)
            .MaximumLength(500)
            .WithMessage("Ghi chú resubmit không được vượt quá 500 ký tự")
            .When(x => !string.IsNullOrEmpty(x.Request.ResubmitNote));
    }
} 