using FluentValidation;
using System.Linq;

namespace Booklify.Application.Features.Book.Commands.CreateBook;

public class CreateBookCommandValidator : AbstractValidator<CreateBookCommand>
{
    public CreateBookCommandValidator()
    {
        RuleFor(x => x.Request.Title)
            .NotEmpty().WithMessage("Tiêu đề không được để trống")
            .MaximumLength(200).WithMessage("Tiêu đề không được vượt quá 200 ký tự");

        RuleFor(x => x.Request.Description)
            .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự");

        RuleFor(x => x.Request.Author)
            .NotEmpty().WithMessage("Tác giả không được để trống")
            .MaximumLength(100).WithMessage("Tên tác giả không được vượt quá 100 ký tự");

        RuleFor(x => x.Request.Publisher)
            .MaximumLength(100).WithMessage("Nhà xuất bản không được vượt quá 100 ký tự");

        RuleFor(x => x.Request.ISBN)
            .MaximumLength(20).WithMessage("ISBN không được vượt quá 20 ký tự");

        RuleFor(x => x.Request.CategoryId)
            .NotEmpty().WithMessage("Danh mục không được để trống");

        RuleFor(x => x.Request.Tags)
            .MaximumLength(500).WithMessage("Tags không được vượt quá 500 ký tự");

        RuleFor(x => x.Request.File)
            .NotNull().WithMessage("Tệp sách không được để trống");

        // Validate file extension and size
        When(x => x.Request.File != null, () =>
        {
            RuleFor(x => x.Request.File)
                .Must(file => 
                {
                    var allowedExtensions = new[] { 
                        ".pdf", ".doc", ".docx", 
                        ".epub", // EPUB support
                        ".txt", ".mp4", ".avi", ".mov", ".mkv", ".zip", ".rar"
                    };
                    var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
                    return allowedExtensions.Contains(extension);
                })
                .WithMessage("Định dạng tệp không được hỗ trợ. Định dạng hỗ trợ: PDF, Word, EPUB, TXT, Video (MP4, AVI, MOV, MKV), Archive (ZIP, RAR)");

            RuleFor(x => x.Request.File)
                .Must(file => {
                    var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
                    var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".epub", ".mp4", ".avi", ".mov", ".mkv", ".zip", ".rar" };
                    
                    // For all allowed file types, support up to 500MB
                    if (allowedExtensions.Contains(extension))
                    {
                        return file.Length <= 500 * 1024 * 1024; // 500MB limit
                    }
                    
                    return false; // Unknown file type
                })
                .WithMessage("Kích thước tệp không được vượt quá 500MB");
        });
    }
} 