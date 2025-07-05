using FluentValidation;
using System.Linq;
using Booklify.Application.Common.Interfaces;

namespace Booklify.Application.Features.Book.Commands.CreateBook;

public class CreateBookCommandValidator : AbstractValidator<CreateBookCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateBookCommandValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        
        RuleFor(x => x.Request.CategoryId)
            .NotEmpty().WithMessage("Danh mục không được để trống");

        RuleFor(x => x.Request.Tags)
            .MaximumLength(500).WithMessage("Tags không được vượt quá 500 ký tự");

        RuleFor(x => x.Request.Isbn)
            .MaximumLength(20).WithMessage("ISBN không được vượt quá 20 ký tự")
            .MustAsync(async (isbn, cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(isbn))
                    return true; // ISBN is optional

                // Check ISBN uniqueness
                var isExists = await _unitOfWork.BookRepository.AnyAsync(b => b.ISBN == isbn);
                return !isExists;
            })
            .WithMessage("ISBN đã tồn tại trong hệ thống");

        RuleFor(x => x.Request.File)
            .NotNull().WithMessage("Tệp sách không được để trống");

        // Validate file extension and size
        When(x => x.Request.File != null, () =>
        {
            RuleFor(x => x.Request.File)
                .Must(file => 
                {
                    var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
                    return extension == ".epub"; // Only EPUB supported
                })
                .WithMessage("Hệ thống chỉ hỗ trợ file EPUB");

            RuleFor(x => x.Request.File)
                .Must(file => file.Length <= 500 * 1024 * 1024) // 500MB limit
                .WithMessage("Kích thước tệp không được vượt quá 500MB");
        });
    }
} 