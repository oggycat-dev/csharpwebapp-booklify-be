using FluentValidation;

namespace Booklify.Application.Features.User.Commands.UpdateProfile;

public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    private readonly string[] ALLOWED_EXTENSIONS = { ".jpg", ".jpeg", ".png" };
    private const int MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB

    public UpdateUserProfileCommandValidator()
    {
        // Conditional validation - only validate if field is provided
        RuleFor(x => x.Request.Phone)
            .Matches(@"^[0-9]{10}$").WithMessage("Phone number must be exactly 10 digits")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Phone));

        RuleFor(x => x.Request.Gender)
            .IsInEnum().WithMessage("Invalid gender")
            .When(x => x.Request.Gender.HasValue);
            
        RuleFor(x => x.Request.Birthday)
            .LessThan(DateTime.UtcNow).WithMessage("Birthday cannot be in the future")
            .When(x => x.Request.Birthday.HasValue);

        // Validate avatar file if provided
        RuleFor(x => x.Request.Avatar)
            .Must(file => file == null || ALLOWED_EXTENSIONS.Contains(Path.GetExtension(file.FileName).ToLower()))
            .WithMessage($"Avatar must be one of the following formats: {string.Join(", ", ALLOWED_EXTENSIONS)}")
            .When(x => x.Request.Avatar != null);

        RuleFor(x => x.Request.Avatar)
            .Must(file => file == null || file.Length <= MAX_FILE_SIZE)
            .WithMessage($"Avatar size must not exceed {MAX_FILE_SIZE / 1024 / 1024}MB")
            .When(x => x.Request.Avatar != null);
    }
} 