using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Web;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Entities.Identity;

namespace Booklify.Application.Features.Auth.Commands.ResendEmailConfirmation;

/// <summary>
/// Handler for resending email confirmation
/// </summary>
public class ResendEmailConfirmationCommandHandler : IRequestHandler<ResendEmailConfirmationCommand, Result>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResendEmailConfirmationCommandHandler> _logger;

    public ResendEmailConfirmationCommandHandler(
        UserManager<AppUser> userManager,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<ResendEmailConfirmationCommandHandler> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result> Handle(ResendEmailConfirmationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find user by email
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                // Don't reveal that user doesn't exist for security
                _logger.LogWarning("Resend email confirmation requested for non-existent email: {Email}", request.Email);
                return Result.Success("If the email exists, verification email has been sent");
            }

            // Check if email is already confirmed
            if (user.EmailConfirmed)
            {
                return Result.Failure("Email already confirmed", ErrorCode.EmailAlreadyConfirmed);
            }

            // Generate new email confirmation token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = HttpUtility.UrlEncode(token);
            
            // Get frontend URL from configuration
            var frontendUrl = _configuration["FrontendUrl"];
            if (string.IsNullOrEmpty(frontendUrl))
            {
                _logger.LogError("FrontendUrl not configured");
                return Result.Failure("Email service configuration error", ErrorCode.InternalError);
            }

            // Create verification link
            var verificationLink = $"{frontendUrl}/confirm-email?email={HttpUtility.UrlEncode(user.Email!)}&token={encodedToken}";

            // Send verification email
            var emailResult = await _emailService.SendEmailVerificationAsync(user.Email!, verificationLink);
            if (!emailResult.IsSuccess)
            {
                _logger.LogError("Failed to send verification email to {Email}: {Error}", user.Email, emailResult.Message);
                return Result.Failure("Failed to send verification email", ErrorCode.EmailSendFailed);
            }

            _logger.LogInformation("Verification email resent successfully to {Email}", user.Email);
            return Result.Success("Verification email sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while resending email confirmation for {Email}", request.Email);
            return Result.Failure("An error occurred while sending verification email", ErrorCode.InternalError);
        }
    }
} 