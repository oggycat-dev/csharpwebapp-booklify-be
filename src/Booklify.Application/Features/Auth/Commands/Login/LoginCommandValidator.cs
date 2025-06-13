using Booklify.Application.Features.Auth.Commands.Login;
using FluentValidation;

namespace Booklify.Application.Features.Auth.Commands.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.LoginRequest.Username)
            .NotEmpty()
            .WithMessage("Username is required");
            
        RuleFor(x => x.LoginRequest.Password)
            .NotEmpty()
            .WithMessage("Password is required");
    }
}