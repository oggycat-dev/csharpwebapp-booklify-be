using FluentValidation;

namespace Booklify.Application.Features.BookCategory.Commands.DeleteBookCategory;

public class DeleteBookCategoryCommandValidator : AbstractValidator<DeleteBookCategoryCommand>
{
    public DeleteBookCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category ID is required");
    }
} 