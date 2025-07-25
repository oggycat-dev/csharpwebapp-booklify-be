using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Entities.Identity;

namespace Booklify.Application.Features.Auth.Commands.ConfirmEmail;

/// <summary>
/// Handler for confirming user email
/// </summary>
public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, Result>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ConfirmEmailCommandHandler> _logger;

    public ConfirmEmailCommandHandler(
        UserManager<AppUser> userManager,
        IIdentityService identityService,
        IEmailService emailService,
        ILogger<ConfirmEmailCommandHandler> logger)
    {
        _userManager = userManager;
        _identityService = identityService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find user by email
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return Result.Failure("User not found", ErrorCode.UserNotFound);
            }

            // Check if email is already confirmed
            if (user.EmailConfirmed)
            {
                return Result.Failure("Email already confirmed", ErrorCode.EmailAlreadyConfirmed);
            }

            // Confirm email with token
            var result = await _userManager.ConfirmEmailAsync(user, request.Token);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning("Email confirmation failed for {Email}: {Errors}", 
                    request.Email, string.Join(", ", errors));
                return Result.Failure("Invalid or expired token", ErrorCode.InvalidEmailToken, errors);
            }

            // Activate user account after email confirmation
            user.IsActive = true;
            var updateResult = await _identityService.UpdateUserAsync(user);
            if (!updateResult.IsSuccess)
            {
                _logger.LogError("Failed to activate user {Email} after email confirmation", request.Email);
                return Result.Failure("Failed to activate account", ErrorCode.InternalError);
            }

            _logger.LogInformation("Email confirmed successfully for user {Email}", request.Email);

            // Send welcome email (don't fail if this fails)
            try
            {
                await _emailService.SendWelcomeEmailAsync(user.Email!, user.UserName ?? "User");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send welcome email to {Email}", user.Email);
                // Don't fail the whole operation for this
            }

            return Result.Success("Email confirmed successfully. Your account is now active!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while confirming email for {Email}", request.Email);
            return Result.Failure("An error occurred during email confirmation", ErrorCode.InternalError);
        }
    }
} 