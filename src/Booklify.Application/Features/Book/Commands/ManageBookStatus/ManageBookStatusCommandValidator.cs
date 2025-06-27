using Booklify.Domain.Enums;
using FluentValidation;

namespace Booklify.Application.Features.Book.Commands.ManageBookStatus;

public class ManageBookStatusCommandValidator : AbstractValidator<ManageBookStatusCommand>
{
    public ManageBookStatusCommandValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty()
            .WithMessage("ID sách không được để trống");

        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Dữ liệu yêu cầu không được để trống");

        // Validate that at least one field is provided
        RuleFor(x => x.Request)
            .Must(request => request.ApprovalStatus.HasValue || request.IsPremium.HasValue)
            .WithMessage("Phải cung cấp ít nhất một trong các trường: trạng thái phê duyệt hoặc trạng thái premium");

        // Validate approval note is required when rejecting
        RuleFor(x => x.Request.ApprovalNote)
            .NotEmpty()
            .When(x => x.Request.ApprovalStatus == ApprovalStatus.Rejected)
            .WithMessage("Ghi chú phê duyệt là bắt buộc khi từ chối sách");

        // Validate approval note max length
        RuleFor(x => x.Request.ApprovalNote)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Request.ApprovalNote))
            .WithMessage("Ghi chú phê duyệt không được vượt quá 500 ký tự");
    }
} 