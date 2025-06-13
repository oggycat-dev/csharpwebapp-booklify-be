using Booklify.Application.Common.Interfaces;
using FluentValidation;

namespace Booklify.Application.Features.BookCategory.Commands.CreateBookCategory;

public class CreateBookCategoryCommandValidator : AbstractValidator<CreateBookCategoryCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public CreateBookCategoryCommandValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters")
            .MustAsync(async (name, cancellationToken) =>
            {
                var isExist = await _unitOfWork.BookCategoryRepository.AnyAsync(x => x.Name == name);
                return !isExist;
            })
            .WithMessage("Book category name already exists");
            
        RuleFor(x => x.Request.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");
    }
} 