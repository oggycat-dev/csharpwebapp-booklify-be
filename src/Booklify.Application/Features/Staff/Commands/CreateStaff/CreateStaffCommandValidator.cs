using Booklify.Application.Common.DTOs.Staff;
using Booklify.Application.Common.Interfaces;
using FluentValidation;

namespace Booklify.Application.Features.Staff.Commands.CreateStaff;

public class CreateStaffCommandValidator : AbstractValidator<CreateStaffCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    public CreateStaffCommandValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        RuleFor(x => x.Request.FirstName).NotEmpty().WithMessage("First name is required");
        RuleFor(x => x.Request.LastName).NotEmpty().WithMessage("Last name is required");
        RuleFor(x => x.Request.StaffCode).NotEmpty().WithMessage("Staff code is required")
            .MustAsync(async (staffCode, cancellationToken) =>
            {
                var isExist = await _unitOfWork.StaffProfileRepository.AnyAsync(x => x.StaffCode == staffCode);
                return !isExist;
            })
            .WithMessage("Staff code already exists");
        RuleFor(x => x.Request.Email).NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email address");
        RuleFor(x => x.Request.Email).MustAsync(async (email, cancellationToken) =>
            {
                var isExist = await _unitOfWork.StaffProfileRepository.AnyAsync(x => x.Email == email);
                return !isExist;
            })
            .WithMessage("Email already exists");
        RuleFor(x => x.Request.Phone).NotEmpty().WithMessage("Phone is required")
            .Matches(@"^[0-9]{10}$")
            .WithMessage("Phone number must be exactly 10 digits")
            .MustAsync(async (phone, cancellationToken) =>
            {
                var isExist = await _unitOfWork.StaffProfileRepository.AnyAsync(x => x.Phone == phone);
                return !isExist;
            })
            .WithMessage("Phone number already exists");
        RuleFor(x => x.Request.Address).NotEmpty().WithMessage("Address is required");
        RuleFor(x => x.Request.Password).NotEmpty().WithMessage("Password is required");
        RuleFor(x => x.Request.Password).MinimumLength(8).WithMessage("Password must be at least 8 characters long");
        RuleFor(x => x.Request.Password).Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$")
            .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character");
        RuleFor(x => x.Request.Position).IsInEnum().WithMessage("Invalid position");
    }
}