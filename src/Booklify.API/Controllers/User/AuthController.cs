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
    
    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
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
        Description = "API xác thực email thông qua token",
        OperationId = "User_ConfirmEmail",
        Tags = new[] { "User", "User_Auth" }
    )]
    public async Task<IActionResult> ConfirmEmail(string email, string token)
    {
        var command = new ConfirmEmailCommand(email, token);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
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
}

/// <summary>
/// Request model for resending email confirmation
/// </summary>
public class ResendEmailRequest
{
    public string Email { get; set; } = string.Empty;
} 