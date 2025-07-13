using FluentValidation;
using Booklify.Application.Common.DTOs.Subscription;

namespace Booklify.Application.Features.User.Commands.ManageSubscription;

public class ManageSubscriptionCommandValidator : AbstractValidator<ManageSubscriptionCommand>
{
    public ManageSubscriptionCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request is required")
            .SetValidator(new SubscriptionManagementRequestValidator());
    }
}

public class SubscriptionManagementRequestValidator : AbstractValidator<SubscriptionManagementRequest>
{
    public SubscriptionManagementRequestValidator()
    {
        RuleFor(x => x.Action)
            .IsInEnum()
            .WithMessage("Invalid action");

        // Validation for Extend action
        When(x => x.Action == SubscriptionAction.Extend, () =>
        {
            RuleFor(x => x.DurationDays)
                .NotNull()
                .WithMessage("Duration days is required for extend action")
                .GreaterThan(0)
                .WithMessage("Duration days must be greater than 0")
                .LessThanOrEqualTo(365)
                .WithMessage("Duration days cannot exceed 365");
        });

        // Validation for Gift action
        When(x => x.Action == SubscriptionAction.Gift, () =>
        {
            RuleFor(x => x.SubscriptionId)
                .NotNull()
                .WithMessage("Subscription ID is required for gift action");

            RuleFor(x => x.DurationDays)
                .NotNull()
                .WithMessage("Duration days is required for gift action")
                .GreaterThan(0)
                .WithMessage("Duration days must be greater than 0")
                .LessThanOrEqualTo(365)
                .WithMessage("Duration days cannot exceed 365");
        });

        // Validation for ReSubscription action
        When(x => x.Action == SubscriptionAction.ReSubscription, () =>
        {
            RuleFor(x => x.SubscriptionId)
                .NotNull()
                .WithMessage("Subscription ID is required for re-subscription action");

            RuleFor(x => x.DurationDays)
                .NotNull()
                .WithMessage("Duration days is required for re-subscription action")
                .GreaterThan(0)
                .WithMessage("Duration days must be greater than 0")
                .LessThanOrEqualTo(365)
                .WithMessage("Duration days cannot exceed 365");

            RuleFor(x => x.PaymentProofUrl)
                .NotEmpty()
                .WithMessage("Payment proof URL is required for re-subscription action")
                .Must(BeValidUrl)
                .WithMessage("Payment proof URL must be a valid URL");

            RuleFor(x => x.PaymentAmount)
                .GreaterThan(0)
                .WithMessage("Payment amount must be greater than 0");
        });

        // Validation for Cancel action
        When(x => x.Action == SubscriptionAction.Cancel, () =>
        {
            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("Reason is required for cancel action")
                .MaximumLength(500)
                .WithMessage("Reason cannot exceed 500 characters");
        });

        // Optional fields validation
        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Reason cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Reason));

        RuleFor(x => x.PaymentMethod)
            .MaximumLength(100)
            .WithMessage("Payment method cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.PaymentMethod));
    }

    private bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
} 