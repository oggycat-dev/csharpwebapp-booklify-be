using FluentValidation;

namespace Booklify.Application.Features.Staff.Commands.UpdateStaff;

public class UpdateStaffCommandValidator : AbstractValidator<UpdateStaffCommand>
{
    public UpdateStaffCommandValidator()
    {
        RuleFor(x => x.StaffId)
            .NotEmpty().WithMessage("Staff ID is required");

        // Conditional validation - only validate if field is provided
        RuleFor(x => x.Request.Email)
            .EmailAddress().WithMessage("Invalid email address")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Email));

        RuleFor(x => x.Request.Phone)
            .Matches(@"^[0-9]{10}$").WithMessage("Phone number must be exactly 10 digits")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Phone));

        RuleFor(x => x.Request.Position)
            .IsInEnum().WithMessage("Invalid position")
            .When(x => x.Request.Position.HasValue);
    }
} 