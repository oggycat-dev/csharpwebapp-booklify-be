using FluentValidation;

namespace Booklify.Application.Features.BookCategory.Commands.UpdateBookCategory;

public class UpdateBookCategoryCommandValidator : AbstractValidator<UpdateBookCategoryCommand>
{
    public UpdateBookCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category ID is required");

        // Conditional validation - only validate if field is provided
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Name cannot be empty")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Name));

        RuleFor(x => x.Request.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Description));
    }
} 