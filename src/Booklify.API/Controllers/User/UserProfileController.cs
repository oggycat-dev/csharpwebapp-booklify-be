using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Booklify.API.Attributes;
using Booklify.API.Configurations;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.DTOs.User;
using Booklify.Application.Features.User.Commands.UpdateProfile;
using Booklify.Application.Features.User.Queries.GetProfile;
using Booklify.Domain.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace Booklify.API.Controllers.User;

/// <summary>
/// Controller quản lý thông tin cá nhân của người dùng
/// </summary>
[ApiController]
[Route("api/user/profile")]
[ApiExplorerSettings(GroupName = "v1")]
[Authorize]
[Configurations.Tags("User", "User_Profile")]
[SwaggerTag("API quản lý thông tin cá nhân của người dùng")]
public class UserProfileController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public UserProfileController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    /// <summary>
    /// Lấy thông tin cá nhân của người dùng hiện tại
    /// </summary>
    /// <remarks>
    /// API này trả về thông tin cá nhân của người dùng đang đăng nhập.
    /// Yêu cầu gửi kèm token xác thực trong header Authorization.
    /// </remarks>
    /// <returns>Thông tin cá nhân của người dùng</returns>
    /// <response code="200">Lấy thông tin thành công</response>
    /// <response code="401">Không có quyền truy cập hoặc token không hợp lệ</response>
    /// <response code="404">Không tìm thấy thông tin người dùng</response>
    [HttpGet]
    [ProducesResponseType(typeof(Result<UserDetailResponse>), 200)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Lấy thông tin cá nhân của người dùng",
        Description = "API lấy thông tin cá nhân của người dùng đang đăng nhập",
        OperationId = "User_GetProfile",
        Tags = new[] { "User", "User_Profile" }
    )]
    public async Task<IActionResult> GetProfile()
    {
        var query = new GetUserProfileQuery();
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }
    
    /// <summary>
    /// Cập nhật thông tin cá nhân (PATCH - Partial Update)
    /// </summary>
    /// <remarks>
    /// API này hỗ trợ cập nhật một phần thông tin cá nhân. Chỉ các trường được gửi lên mới được cập nhật.
    /// 
    /// Mẫu request (multipart/form-data):
    /// 
    ///     PATCH /api/user/profile
    ///     Content-Type: multipart/form-data
    ///     {
    ///        "first_name": "Tên mới",
    ///        "last_name": "Họ mới",
    ///        "phone": "0987654321",
    ///        "address": "Địa chỉ mới",
    ///        "birthday": "1990-01-01",
    ///        "gender": 1,
    ///        "avatar": [file] // Tệp hình ảnh (jpg, jpeg, png) tối đa 5MB
    ///     }
    ///     
    /// Các trường có thể cập nhật:
    /// - first_name: Tên (optional)
    /// - last_name: Họ (optional)
    /// - phone: Số điện thoại (10 số)
    /// - address: Địa chỉ
    /// - birthday: Ngày sinh (định dạng ISO 8601)
    /// - gender: Giới tính (0: Female, 1: Male, 2: Other)
    /// - avatar: Tệp hình ảnh đại diện (jpg, jpeg, png) tối đa 5MB
    ///     
    /// Lưu ý: 
    /// - Validation sẽ bị skip cho các trường không được gửi lên
    /// - Nếu gửi avatar mới, avatar cũ sẽ bị xóa
    /// - Chỉ hỗ trợ định dạng ảnh: jpg, jpeg, png
    /// - Kích thước file tối đa: 5MB
    /// </remarks>
    /// <returns>Thông báo kết quả cập nhật</returns>
    /// <response code="200">Cập nhật thành công</response>
    /// <response code="400">Dữ liệu không hợp lệ hoặc không có thay đổi</response>
    /// <response code="401">Không có quyền truy cập hoặc token không hợp lệ</response>
    /// <response code="404">Không tìm thấy thông tin người dùng</response>
    [HttpPatch]
    [SkipModelValidation] // Skip model validation for partial updates
    [Consumes("multipart/form-data")] // Support file upload
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Cập nhật thông tin cá nhân (Partial Update)",
        Description = "API cập nhật một phần thông tin cá nhân. Model validation sẽ bị skip, chỉ validate các trường được gửi lên.",
        OperationId = "User_UpdateProfile",
        Tags = new[] { "User", "User_Profile" }
    )]
    public async Task<IActionResult> UpdateProfile(
        [FromForm(Name = "first_name")] string? firstName = null,
        [FromForm(Name = "last_name")] string? lastName = null,
        [FromForm(Name = "phone")] string? phone = null,
        [FromForm(Name = "address")] string? address = null,
        [FromForm(Name = "birthday")] DateTime? birthday = null,
        [FromForm(Name = "gender")] Gender? gender = null)
    {
        var request = new UpdateUserProfileRequest
        {
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            Address = address,
            Birthday = birthday,
            Gender = gender
        };
        
        // Get the avatar file directly from the form if exists
        var avatar = Request.Form.Files.GetFile("avatar");
        if (avatar != null)
        {
            request.Avatar = avatar;
        }
        
        var command = new UpdateUserProfileCommand(request);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }
} 