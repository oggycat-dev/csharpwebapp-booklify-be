using Booklify.Application.Common.Models;

namespace Booklify.Application.Common.Interfaces;

/// <summary>
/// Interface for email service operations
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send a generic email
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlContent">HTML content of the email</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> SendEmailAsync(string toEmail, string subject, string htmlContent);

    /// <summary>
    /// Send email verification email
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="verificationLink">Email verification link</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> SendEmailVerificationAsync(string toEmail, string verificationLink);

    /// <summary>
    /// Send password reset email
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="resetLink">Password reset link</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> SendPasswordResetAsync(string toEmail, string resetLink);

    /// <summary>
    /// Send welcome email after successful registration
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="userName">User's display name</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> SendWelcomeEmailAsync(string toEmail, string userName);
} 