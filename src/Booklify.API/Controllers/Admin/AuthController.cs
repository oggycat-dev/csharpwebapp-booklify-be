using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Booklify.API.Configurations;
using Booklify.API.Middlewares;
using Booklify.Application.Features.Auth.Commands.Login;
using Booklify.Application.Features.Auth.Commands.ChangePassword;
using Booklify.Application.Features.Auth.Commands.Logout;
using Booklify.Application.Features.Auth.Queries.ReAuthenticate;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.DTOs.Auth;
using Swashbuckle.AspNetCore.Annotations;

namespace Booklify.API.Controllers.Admin;

/// <summary>
/// Controller quản lý xác thực cho ứng dụng quản trị viên
/// </summary>
[ApiController]
[Route("api/cms/auth")]
[ApiExplorerSettings(GroupName = "admin")]
[Configurations.Tags("Admin", "Admin_Auth")]
[SwaggerTag("API xác thực dành cho quản trị viên")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    /// <summary>
    /// Đăng nhập vào hệ thống quản trị
    /// </summary>
    /// <remarks>
    /// Mẫu request:
    /// 
    ///     POST /api/cms/auth/login
    ///     {
    ///        "username": "admin",
    ///        "password": "admin123",
    ///        "grant_type": "password"
    ///     }
    ///     
    /// Trường `grant_type` mặc định là "password"
    /// </remarks>
    /// <param name="request">Thông tin đăng nhập</param>
    /// <returns>Thông tin người dùng và token xác thực</returns>
    /// <response code="200">Đăng nhập thành công</response>
    /// <response code="400">Lỗi đăng nhập (tên đăng nhập hoặc mật khẩu không đúng)</response>
    /// <response code="403">Không có quyền truy cập (người dùng không phải quản trị viên)</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ServiceFilter(typeof(AdminRoleAccessFilter))]
    [ProducesResponseType(typeof(Result<AuthenticationResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 403)]
    [SwaggerOperation(
        Summary = "Đăng nhập vào hệ thống quản trị",
        Description = "API đăng nhập dành cho quản trị viên",
        OperationId = "Admin_Login",
        Tags = new[] { "Admin", "Admin_Auth" }
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
        OperationId = "Admin_RefreshToken",
        Tags = new[] { "Admin", "Admin_Auth" }
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
    ///     POST /api/cms/auth/change-password
    ///     {
    ///        "old_password": "admin123",
    ///        "new_password": "newPassword123"
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
        OperationId = "Admin_ChangePassword",
        Tags = new[] { "Admin", "Admin_Auth" }
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
        OperationId = "Admin_Logout",
        Tags = new[] { "Admin", "Admin_Auth" }
    )]
    public async Task<IActionResult> Logout()
    {
        var command = new LogoutCommand();
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }
} 