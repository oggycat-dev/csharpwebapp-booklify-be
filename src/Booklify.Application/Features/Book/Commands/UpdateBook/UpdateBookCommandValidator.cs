using FluentValidation;

namespace Booklify.Application.Features.Book.Commands.UpdateBook;

public class UpdateBookCommandValidator : AbstractValidator<UpdateBookCommand>
{
    public UpdateBookCommandValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty().WithMessage("Book ID is required");

        // Conditional validation - only validate if field is provided
        RuleFor(x => x.Request.Title)
            .NotEmpty().WithMessage("Tiêu đề sách không được để trống")
            .MaximumLength(500).WithMessage("Tiêu đề sách không được vượt quá 500 ký tự")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Title));

        RuleFor(x => x.Request.Author)
            .NotEmpty().WithMessage("Tác giả không được để trống")
            .MaximumLength(200).WithMessage("Tên tác giả không được vượt quá 200 ký tự")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Author));

        RuleFor(x => x.Request.ISBN)
            .MaximumLength(20).WithMessage("Mã ISBN không được vượt quá 20 ký tự")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.ISBN));

        RuleFor(x => x.Request.Publisher)
            .MaximumLength(200).WithMessage("Tên nhà xuất bản không được vượt quá 200 ký tự")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Publisher));

        RuleFor(x => x.Request.Description)
            .MaximumLength(2000).WithMessage("Mô tả sách không được vượt quá 2000 ký tự")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Description));

        RuleFor(x => x.Request.Tags)
            .MaximumLength(500).WithMessage("Tags không được vượt quá 500 ký tự")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Tags));

        RuleFor(x => x.Request.CategoryId)
            .NotEmpty().WithMessage("ID danh mục sách không hợp lệ")
            .When(x => x.Request.CategoryId.HasValue);


        RuleFor(x => x.Request.Status)
            .IsInEnum().WithMessage("Trạng thái sách không hợp lệ")
            .When(x => x.Request.Status.HasValue);
    }
} 