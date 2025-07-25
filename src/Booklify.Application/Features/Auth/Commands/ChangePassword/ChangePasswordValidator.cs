using Booklify.Application.Features.Auth.Commands.ChangePassword;
using FluentValidation;

namespace Booklify.Application.Features.Auth.Commands.ChangePassword;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.Request.OldPassword)
            .NotEmpty()
            .WithMessage("Old password is required");

        RuleFor(x => x.Request.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required");
            
    }
}