using FluentValidation;

namespace Booklify.Application.Features.Subscription.Commands.CreateSubscription;

public class CreateSubscriptionCommandValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Subscription name is required")
            .Length(3, 100).WithMessage("Subscription name must be between 3 and 100 characters")
            .Matches("^[a-zA-Z0-9\\s\\-_]+$").WithMessage("Subscription name can only contain letters, numbers, spaces, hyphens and underscores");

        RuleFor(x => x.Request.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.Request.Price)
            .GreaterThanOrEqualTo(5000).WithMessage("Price must be at least 5,000 VND (VNPay minimum requirement)")
            .LessThanOrEqualTo(999999999).WithMessage("Price cannot exceed 999,999,999");

        RuleFor(x => x.Request.Duration)
            .GreaterThan(0).WithMessage("Duration must be greater than 0")
            .LessThanOrEqualTo(365).WithMessage("Duration cannot exceed 365 days");

        RuleFor(x => x.Request.Features)
            .Must(features => features == null || features.Count <= 20)
            .WithMessage("Cannot have more than 20 features")
            .Must(features => features == null || features.All(f => !string.IsNullOrWhiteSpace(f) && f.Length <= 100))
            .WithMessage("Each feature must be non-empty and not exceed 100 characters");

        RuleFor(x => x.Request.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be non-negative");
    }
} 