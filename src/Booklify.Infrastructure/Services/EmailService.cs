using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Infrastructure.Models;

namespace Booklify.Infrastructure.Services;

/// <summary>
/// Email service implementation using MailKit
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailSettings> emailSettings,
        ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Send a generic email
    /// </summary>
    public async Task<Result> SendEmailAsync(string toEmail, string subject, string htmlContent)
    {
        try
        {
            if (string.IsNullOrEmpty(toEmail) || !IsValidEmail(toEmail))
            {
                return Result.Failure("Invalid email address", ErrorCode.InvalidInput);
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlContent
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            // Kết nối đến SMTP server với đúng SecureSocketOptions
            SecureSocketOptions secureOptions;
            if (_emailSettings.Port == 465)
            {
                // Port 465 dùng SSL/TLS ngay từ đầu
                secureOptions = SecureSocketOptions.SslOnConnect;
            }
            else if (_emailSettings.Port == 587)
            {
                // Port 587 dùng STARTTLS (bắt đầu plain text rồi upgrade to TLS)
                secureOptions = SecureSocketOptions.StartTls;
            }
            else
            {
                // Fallback to auto-detect
                secureOptions = _emailSettings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            }
            
            await client.ConnectAsync(_emailSettings.Host, _emailSettings.Port, secureOptions);
            
            // Xác thực
            if (!string.IsNullOrEmpty(_emailSettings.Username) && !string.IsNullOrEmpty(_emailSettings.Password))
            {
                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
            }

            // Gửi email
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {Email} with subject '{Subject}'", toEmail, subject);
            return Result.Success("Email sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending email to {Email}", toEmail);
            return Result.Failure($"Email sending failed: {ex.Message}", ErrorCode.EmailSendFailed);
        }
    }

    /// <summary>
    /// Send email verification email
    /// </summary>
    public async Task<Result> SendEmailVerificationAsync(string toEmail, string verificationLink)
    {
        var subject = "Xác thực tài khoản Booklify";
        var htmlContent = GetEmailVerificationTemplate(verificationLink);

        return await SendEmailAsync(toEmail, subject, htmlContent);
    }

    /// <summary>
    /// Send password reset email
    /// </summary>
    public async Task<Result> SendPasswordResetAsync(string toEmail, string resetLink)
    {
        var subject = "Đặt lại mật khẩu Booklify";
        var htmlContent = GetPasswordResetTemplate(resetLink);

        return await SendEmailAsync(toEmail, subject, htmlContent);
    }

    /// <summary>
    /// Send welcome email after successful registration
    /// </summary>
    public async Task<Result> SendWelcomeEmailAsync(string toEmail, string userName)
    {
        var subject = "Chào mừng bạn đến với Booklify!";
        var htmlContent = GetWelcomeEmailTemplate(userName);

        return await SendEmailAsync(toEmail, subject, htmlContent);
    }

    #region Private Helper Methods

    /// <summary>
    /// Get email verification template
    /// </summary>
    private string GetEmailVerificationTemplate(string verificationLink)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Xác thực tài khoản Booklify</title>
            </head>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                <div style='text-align: center; margin-bottom: 30px;'>
                    <h1 style='color: #007bff;'>📚 Booklify</h1>
                </div>
                
                <div style='background-color: #f8f9fa; padding: 30px; border-radius: 10px;'>
                    <h2>Xác thực tài khoản của bạn</h2>
                    <p>Chào bạn,</p>
                    <p>Cảm ơn bạn đã đăng ký tài khoản Booklify! Vui lòng nhấn vào nút bên dưới để xác thực tài khoản.</p>
                    
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{verificationLink}' 
                           style='background-color: #007bff; color: white; padding: 15px 30px; 
                                  text-decoration: none; border-radius: 5px; display: inline-block;'>
                            ✅ Xác thực tài khoản
                        </a>
                    </div>
                    
                    <p style='color: #666; font-size: 14px;'>
                        Liên kết này sẽ hết hạn sau 24 giờ.
                    </p>
                </div>
                
                <div style='text-align: center; margin-top: 30px;'>
                    <p style='color: #999; font-size: 12px;'>
                        © 2024 Booklify. Mọi quyền được bảo lưu.
                    </p>
                </div>
            </body>
            </html>";
    }

    /// <summary>
    /// Get password reset template
    /// </summary>
    private string GetPasswordResetTemplate(string resetLink)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Đặt lại mật khẩu Booklify</title>
            </head>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                <div style='text-align: center; margin-bottom: 30px;'>
                    <h1 style='color: #007bff;'>📚 Booklify</h1>
                </div>
                
                <div style='background-color: #f8f9fa; padding: 30px; border-radius: 10px;'>
                    <h2>🔒 Đặt lại mật khẩu</h2>
                    <p>Chào bạn,</p>
                    <p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản Booklify. Nhấn vào nút bên dưới để tiếp tục.</p>
                    
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{resetLink}' 
                           style='background-color: #dc3545; color: white; padding: 15px 30px; 
                                  text-decoration: none; border-radius: 5px; display: inline-block;'>
                            🔑 Đặt lại mật khẩu
                        </a>
                    </div>
                    
                    <p style='color: #666; font-size: 14px;'>
                        Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.
                    </p>
                </div>
            </body>
            </html>";
    }

    /// <summary>
    /// Get welcome email template
    /// </summary>
    private string GetWelcomeEmailTemplate(string userName)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Chào mừng đến với Booklify</title>
            </head>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                <div style='text-align: center; margin-bottom: 30px;'>
                    <h1 style='color: #007bff;'>📚 Booklify</h1>
                </div>
                
                <div style='background-color: #d4edda; padding: 30px; border-radius: 10px;'>
                    <h2>🎉 Chào mừng đến với Booklify!</h2>
                    <p>Xin chào {userName},</p>
                    <p>Tài khoản của bạn đã được xác thực thành công! Chào mừng bạn đến với cộng đồng đọc sách Booklify.</p>
                    
                    <div style='margin: 30px 0;'>
                        <h3>Những gì bạn có thể làm với Booklify:</h3>
                        <ul style='text-align: left; color: #333;'>
                            <li>📖 Đọc hàng ngàn cuốn sách chất lượng</li>
                            <li>🔖 Lưu sách yêu thích và tiếp tục đọc sau</li>
                            <li>💬 Tham gia thảo luận về sách</li>
                            <li>⭐ Đánh giá và bình luận sách</li>
                        </ul>
                    </div>
                    
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='#' 
                           style='background-color: #28a745; color: white; padding: 15px 30px; 
                                  text-decoration: none; border-radius: 5px; display: inline-block;'>
                            🚀 Bắt đầu đọc ngay
                        </a>
                    </div>
                </div>
                
                <div style='text-align: center; margin-top: 30px;'>
                    <p style='color: #999; font-size: 12px;'>
                        © 2024 Booklify. Mọi quyền được bảo lưu.
                    </p>
                </div>
            </body>
            </html>";
    }

    /// <summary>
    /// Validate email address format
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    #endregion
} 