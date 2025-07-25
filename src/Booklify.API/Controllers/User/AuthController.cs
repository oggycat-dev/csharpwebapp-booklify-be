using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Booklify.API.Configurations;
using Booklify.Application.Features.Auth.Commands.Login;
using Booklify.Application.Features.Auth.Commands.Register;
using Booklify.Application.Features.Auth.Commands.ChangePassword;
using Booklify.Application.Features.Auth.Commands.Logout;
using Booklify.Application.Features.Auth.Commands.ConfirmEmail;
using Booklify.Application.Features.Auth.Commands.ResendEmailConfirmation;
using Booklify.Application.Features.Auth.Queries.ReAuthenticate;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.DTOs.Auth;
using Swashbuckle.AspNetCore.Annotations;

namespace Booklify.API.Controllers.User;

/// <summary>
/// Controller quản lý xác thực cho người dùng
/// </summary>
[ApiController]
[Route("api/auth")]
[ApiExplorerSettings(GroupName = "v1")]
[Configurations.Tags("User", "User_Auth")]
[SwaggerTag("API xác thực dành cho người dùng")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    
    public AuthController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }
    
    /// <summary>
    /// Đăng ký tài khoản mới
    /// </summary>
    /// <remarks>
    /// Đăng ký tài khoản người dùng mới vào hệ thống.
    /// 
    /// Mẫu request:
    /// 
    ///     POST /api/auth/register
    ///     {
    ///        "first_name": "Nam",
    ///        "last_name": "Nguyen",
    ///        "gender": 1,
    ///        "username": "user123",
    ///        "email": "user@example.com",
    ///        "password": "Password123",
    ///        "phone_number": "0123456789"
    ///     }
    /// 
    /// Mật khẩu phải chứa ít nhất 6 ký tự, bao gồm chữ hoa, chữ thường và số. Gender 0 là Female, 1 là Male, 2 là Other
    /// </remarks>
    /// <param name="request">Thông tin đăng ký</param>
    /// <returns>Thông tin tài khoản đã tạo</returns>
    /// <response code="200">Đăng ký thành công</response>
    /// <response code="400">Lỗi đăng ký (email đã tồn tại, mật khẩu không hợp lệ, v.v.)</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<UserRegistrationResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [SwaggerOperation(
        Summary = "Đăng ký tài khoản mới",
        Description = "API đăng ký tài khoản nrgười dùng mới",
        OperationId = "User_Register",
        Tags = new[] { "User", "User_Auth" }
    )]
    public async Task<IActionResult> Register([FromBody] UserRegistrationRequest request)
    {
        var command = new RegisterUserCommand(request);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }
    
    /// <summary>
    /// Đăng nhập vào hệ thống
    /// </summary>
    /// <remarks>
    /// Đăng nhập vào hệ thống với tên đăng nhập và mật khẩu.
    /// 
    /// Mẫu request:
    /// 
    ///     POST /api/auth/login
    ///     {
    ///        "username": "user123",
    ///        "password": "Password123",
    ///        "grant_type": "password"
    ///     }
    ///     
    /// Trường `grant_type` mặc định là "password"
    /// </remarks>
    /// <param name="request">Thông tin đăng nhập</param>
    /// <returns>Thông tin người dùng và token xác thực</returns>
    /// <response code="200">Đăng nhập thành công</response>
    /// <response code="400">Lỗi đăng nhập (tên đăng nhập hoặc mật khẩu không đúng)</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<AuthenticationResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [SwaggerOperation(
        Summary = "Đăng nhập vào hệ thống",
        Description = "API đăng nhập dành cho người dùng",
        OperationId = "User_Login",
        Tags = new[] { "User", "User_Auth" }
    )]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }
    
    /// <summary>
    /// Làm mới token
    /// </summary>
    /// <remarks>
    /// Sử dụng API này để gia hạn token khi token hiện tại gần hết hạn.
    /// Yêu cầu gửi kèm token xác thực trong header Authorization.
    /// 
    /// Ví dụ header:
    ///     Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
    /// </remarks>
    /// <returns>Token mới và thông tin người dùng</returns>
    /// <response code="200">Làm mới token thành công</response>
    /// <response code="401">Token không hợp lệ hoặc hết hạn</response>
    [HttpGet("refresh")]
    [Authorize]
    [ProducesResponseType(typeof(Result<AuthenticationResponse>), 200)]
    [ProducesResponseType(typeof(Result), 401)]
    [SwaggerOperation(
        Summary = "Làm mới token xác thực",
        Description = "Gia hạn token xác thực khi token hiện tại gần hết hạn",
        OperationId = "User_RefreshToken",
        Tags = new[] { "User", "User_Auth" }
    )]
    public async Task<IActionResult> RefreshToken()
    {
        var query = new ReAuthenticateQuery();
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }
    
    /// <summary>
    /// Đổi mật khẩu
    /// </summary>
    /// <remarks>
    /// Đổi mật khẩu cho tài khoản hiện tại.
    /// Yêu cầu gửi kèm token xác thực trong header Authorization.
    /// 
    /// Mẫu request:
    /// 
    ///     POST /api/auth/change-password
    ///     {
    ///        "old_password": "Password123",
    ///        "new_password": "NewPassword123"
    ///     }
    /// </remarks>
    /// <param name="request">Mật khẩu cũ và mật khẩu mới</param>
    /// <returns>Kết quả đổi mật khẩu</returns>
    /// <response code="200">Đổi mật khẩu thành công</response>
    /// <response code="400">Mật khẩu cũ không đúng hoặc mật khẩu mới không hợp lệ</response>
    /// <response code="401">Token không hợp lệ hoặc hết hạn</response>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 401)]
    [SwaggerOperation(
        Summary = "Đổi mật khẩu người dùng",
        Description = "Đổi mật khẩu cho tài khoản hiện tại",
        OperationId = "User_ChangePassword",
        Tags = new[] { "User", "User_Auth" }
    )]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var command = new ChangePasswordCommand(request);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }
    
    /// <summary>
    /// Đăng xuất
    /// </summary>
    /// <remarks>
    /// Đăng xuất khỏi hệ thống và vô hiệu hóa token hiện tại.
    /// Yêu cầu gửi kèm token xác thực trong header Authorization.
    /// </remarks>
    /// <returns>Kết quả đăng xuất</returns>
    /// <response code="200">Đăng xuất thành công</response>
    /// <response code="401">Token không hợp lệ hoặc hết hạn</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 401)]
    [SwaggerOperation(
        Summary = "Đăng xuất",
        Description = "Đăng xuất khỏi hệ thống và vô hiệu hóa token hiện tại",
        OperationId = "User_Logout",
        Tags = new[] { "User", "User_Auth" }
    )]
    public async Task<IActionResult> Logout()
    {
        var command = new LogoutCommand();
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }

    /// <summary>
    /// Xác thực email
    /// </summary>
    /// <remarks>
    /// Xác thực email thông qua token được gửi trong email.
    /// API này được gọi khi người dùng nhấn vào link xác thực trong email.
    /// 
    /// Ví dụ URL từ email:
    ///     GET /api/auth/confirm-email?email=user@example.com&amp;token=ABC123...
    /// </remarks>
    /// <param name="email">Email của người dùng</param>
    /// <param name="token">Token xác thực từ email</param>
    /// <returns>Kết quả xác thực email</returns>
    /// <response code="200">Xác thực email thành công</response>
    /// <response code="400">Token không hợp lệ hoặc hết hạn</response>
    /// <response code="404">Người dùng không tồn tại</response>
    [HttpGet("confirm-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Xác thực email",
        Description = "API xác thực email thông qua token và hiển thị trang xác thực thành công",
        OperationId = "User_ConfirmEmail",
        Tags = new[] { "User", "User_Auth" }
    )]
    public async Task<IActionResult> ConfirmEmail(string email, string token)
    {
        var command = new ConfirmEmailCommand(email, token);
        var result = await _mediator.Send(command);
        
        // Get frontend URL for redirect
        var frontendUrl = GetFrontendUrl();
        var loginUrl = $"{frontendUrl}/";
        
        if (!result.IsSuccess)
        {
            var errorHtml = GenerateErrorPage(result.Message, loginUrl);
            return Content(errorHtml, "text/html");
        }
        
        var successHtml = GenerateSuccessPage(loginUrl);
        return Content(successHtml, "text/html");
    }

    /// <summary>
    /// Gửi lại email xác thực
    /// </summary>
    /// <remarks>
    /// Gửi lại email xác thực cho người dùng chưa xác thực email.
    /// 
    /// Mẫu request:
    /// 
    ///     POST /api/auth/resend-email-confirmation
    ///     {
    ///        "email": "user@example.com"
    ///     }
    /// </remarks>
    /// <param name="request">Email để gửi lại xác thực</param>
    /// <returns>Kết quả gửi email</returns>
    /// <response code="200">Gửi email thành công</response>
    /// <response code="400">Email đã được xác thực hoặc không hợp lệ</response>
    /// <response code="500">Lỗi gửi email</response>
    [HttpPost("resend-email-confirmation")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 500)]
    [SwaggerOperation(
        Summary = "Gửi lại email xác thực",
        Description = "API gửi lại email xác thực cho người dùng",
        OperationId = "User_ResendEmailConfirmation",
        Tags = new[] { "User", "User_Auth" }
    )]
    public async Task<IActionResult> ResendEmailConfirmation([FromBody] ResendEmailRequest request)
    {
        var command = new ResendEmailConfirmationCommand(request.Email);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }
    
    #region Private Helper Methods
    
    /// <summary>
    /// Get frontend URL from configuration
    /// </summary>
    private string GetFrontendUrl()
    {
        var frontendUrl = _configuration["FrontendUrl"];
        
        if (string.IsNullOrEmpty(frontendUrl))
        {
            return "http://localhost:3000"; // Default fallback
        }
        
        // Get first URL if multiple URLs are configured
        var urls = frontendUrl.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return urls[0].Trim();
    }
    
    /// <summary>
    /// Generate success page HTML with orange theme
    /// </summary>
    private string GenerateSuccessPage(string loginUrl)
    {
        return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Xác thực thành công - Booklify</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #ff9500 0%, #ff6b00 100%);
            min-height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
            color: #333;
        }}
        
        .container {{
            background: white;
            border-radius: 20px;
            box-shadow: 0 20px 40px rgba(255, 149, 0, 0.3);
            padding: 50px 40px;
            text-align: center;
            max-width: 500px;
            width: 90%;
            position: relative;
            overflow: hidden;
        }}
        
        .container::before {{
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            height: 5px;
            background: linear-gradient(90deg, #ff9500, #ff6b00, #ff9500);
        }}
        
        .success-icon {{
            width: 80px;
            height: 80px;
            background: linear-gradient(135deg, #ff9500, #ff6b00);
            border-radius: 50%;
            margin: 0 auto 30px;
            display: flex;
            align-items: center;
            justify-content: center;
            position: relative;
            animation: bounce 0.6s ease-out;
        }}
        
        .success-icon::after {{
            content: '✓';
            color: white;
            font-size: 36px;
            font-weight: bold;
        }}
        
        h1 {{
            color: #ff6b00;
            font-size: 2.2em;
            margin-bottom: 20px;
            font-weight: 700;
        }}
        
        .message {{
            color: #666;
            font-size: 1.1em;
            line-height: 1.6;
            margin-bottom: 30px;
        }}
        
        .redirect-info {{
            background: #fff5f0;
            border: 2px solid #ff9500;
            border-radius: 12px;
            padding: 20px;
            margin: 30px 0;
        }}
        
        .redirect-text {{
            color: #ff6b00;
            font-weight: 600;
            margin-bottom: 10px;
        }}
        
        .countdown {{
            font-size: 2em;
            font-weight: bold;
            color: #ff6b00;
            margin: 10px 0;
        }}
        
        .login-button {{
            display: inline-block;
            background: linear-gradient(135deg, #ff9500, #ff6b00);
            color: white;
            padding: 15px 30px;
            text-decoration: none;
            border-radius: 50px;
            font-weight: 600;
            font-size: 1.1em;
            transition: all 0.3s ease;
            box-shadow: 0 4px 15px rgba(255, 149, 0, 0.4);
        }}
        
        .login-button:hover {{
            transform: translateY(-2px);
            box-shadow: 0 6px 20px rgba(255, 149, 0, 0.6);
        }}
        
        .bookify-logo {{
            color: #ff6b00;
            font-size: 1.5em;
            font-weight: bold;
            margin-bottom: 10px;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
        }}
        
        @keyframes bounce {{
            0%, 20%, 50%, 80%, 100% {{ transform: translateY(0); }}
            40% {{ transform: translateY(-10px); }}
            60% {{ transform: translateY(-5px); }}
        }}
        
        @keyframes fadeIn {{
            from {{ opacity: 0; transform: translateY(20px); }}
            to {{ opacity: 1; transform: translateY(0); }}
        }}
        
        .container > * {{
            animation: fadeIn 0.6s ease-out forwards;
        }}
        
        .container > *:nth-child(2) {{ animation-delay: 0.1s; }}
        .container > *:nth-child(3) {{ animation-delay: 0.2s; }}
        .container > *:nth-child(4) {{ animation-delay: 0.3s; }}
        .container > *:nth-child(5) {{ animation-delay: 0.4s; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='bookify-logo'>
            📚 Booklify
        </div>
        
        <div class='success-icon'></div>
        
        <h1>🎉 Xác thực thành công!</h1>
        
        <p class='message'>
            Chúc mừng! Tài khoản của bạn đã được xác thực thành công.<br>
            Bạn có thể bắt đầu khám phá thế giới sách tuyệt vời trên Booklify.
        </p>
        
        <div class='redirect-info'>
            <div class='redirect-text'>Tự động chuyển hướng trong:</div>
            <div class='countdown' id='countdown'>3</div>
            <div style='color: #888; font-size: 0.9em;'>giây</div>
        </div>
        
        <a href='{loginUrl}' class='login-button'>
            🚀 Đi đến trang đăng nhập
        </a>
    </div>

    <script>
        let countdown = 3;
        const countdownElement = document.getElementById('countdown');
        
        const timer = setInterval(() => {{
            countdown--;
            countdownElement.textContent = countdown;
            
            if (countdown <= 0) {{
                clearInterval(timer);
                window.location.href = '{loginUrl}';
            }}
        }}, 1000);
        
        // Allow manual navigation
        document.addEventListener('click', function(e) {{
            if (e.target.classList.contains('login-button')) {{
                clearInterval(timer);
            }}
        }});
    </script>
</body>
</html>";
    }
    
    /// <summary>
    /// Generate error page HTML with orange theme
    /// </summary>
    private string GenerateErrorPage(string errorMessage, string loginUrl)
    {
        return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Lỗi xác thực - Booklify</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #ff9500 0%, #ff6b00 100%);
            min-height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
            color: #333;
        }}
        
        .container {{
            background: white;
            border-radius: 20px;
            box-shadow: 0 20px 40px rgba(255, 149, 0, 0.3);
            padding: 50px 40px;
            text-align: center;
            max-width: 500px;
            width: 90%;
            position: relative;
            overflow: hidden;
        }}
        
        .container::before {{
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            height: 5px;
            background: linear-gradient(90deg, #ff6b00, #ff4444, #ff6b00);
        }}
        
        .error-icon {{
            width: 80px;
            height: 80px;
            background: linear-gradient(135deg, #ff4444, #cc3333);
            border-radius: 50%;
            margin: 0 auto 30px;
            display: flex;
            align-items: center;
            justify-content: center;
            position: relative;
        }}
        
        .error-icon::after {{
            content: '✗';
            color: white;
            font-size: 36px;
            font-weight: bold;
        }}
        
        h1 {{
            color: #ff4444;
            font-size: 2.2em;
            margin-bottom: 20px;
            font-weight: 700;
        }}
        
        .message {{
            color: #666;
            font-size: 1.1em;
            line-height: 1.6;
            margin-bottom: 20px;
        }}
        
        .error-details {{
            background: #fff5f5;
            border: 2px solid #ff4444;
            border-radius: 12px;
            padding: 20px;
            margin: 20px 0;
            color: #cc3333;
        }}
        
        .login-button {{
            display: inline-block;
            background: linear-gradient(135deg, #ff9500, #ff6b00);
            color: white;
            padding: 15px 30px;
            text-decoration: none;
            border-radius: 50px;
            font-weight: 600;
            font-size: 1.1em;
            transition: all 0.3s ease;
            box-shadow: 0 4px 15px rgba(255, 149, 0, 0.4);
            margin-top: 20px;
        }}
        
        .login-button:hover {{
            transform: translateY(-2px);
            box-shadow: 0 6px 20px rgba(255, 149, 0, 0.6);
        }}
        
        .bookify-logo {{
            color: #ff6b00;
            font-size: 1.5em;
            font-weight: bold;
            margin-bottom: 10px;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='bookify-logo'>
            📚 Booklify
        </div>
        
        <div class='error-icon'></div>
        
        <h1>❌ Xác thực thất bại</h1>
        
        <p class='message'>
            Rất tiếc, quá trình xác thực tài khoản không thành công.
        </p>
        
        <div class='error-details'>
            <strong>Lỗi:</strong> {errorMessage}
        </div>
        
        <p class='message'>
            Vui lòng thử lại hoặc liên hệ với chúng tôi để được hỗ trợ.
        </p>
        
        <a href='{loginUrl}' class='login-button'>
            🏠 Về trang đăng nhập
        </a>
    </div>
</body>
</html>";
    }
    
    #endregion
}

/// <summary>
/// Request model for resending email confirmation
/// </summary>
public class ResendEmailRequest
{
    public string Email { get; set; } = string.Empty;
} 