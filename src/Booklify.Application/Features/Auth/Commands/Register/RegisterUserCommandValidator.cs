using FluentValidation;

namespace Booklify.Application.Features.Auth.Commands.Register;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        // RuleFor(v => v.Request.FirstName)
        //     .NotEmpty().WithMessage("First name is required");

        // RuleFor(v => v.Request.LastName)
        //     .NotEmpty().WithMessage("Last name is required");

        RuleFor(v => v.Request.Username)
            .NotEmpty().WithMessage("Username is required")
            .WithMessage("Username is required")
            .Length(3, 50)
            .WithMessage("Username must be between 3 and 50 characters");
            
        RuleFor(v => v.Request.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Valid email address is required");
            
        RuleFor(v => v.Request.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(6)
            .WithMessage("Password must be at least 6 characters")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$")
            .WithMessage("Password must contain at least one lowercase letter, one uppercase letter, and one number");
            
        RuleFor(v => v.Request.PhoneNumber)
            .Matches(@"^[0-9+\-\s()]*$")
            .WithMessage("Phone number format is invalid")
            .When(v => !string.IsNullOrEmpty(v.Request.PhoneNumber));
    }
} 