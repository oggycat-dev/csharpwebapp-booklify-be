using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Booklify.API.Configurations;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.DTOs.Staff;
using Booklify.Application.Features.Staff.Commands.CreateStaff;
using Swashbuckle.AspNetCore.Annotations;

namespace Booklify.API.Controllers.Admin;

/// <summary>
/// Controller quản lý nhân viên cho ứng dụng quản trị viên
/// </summary>
[ApiController]
[Route("api/cms/staff")]
[ApiExplorerSettings(GroupName = "v1")]
[Authorize(Roles = "Admin")]
[Configurations.Tags("Admin", "Admin_Staff")]
[SwaggerTag("API quản lý nhân viên dành cho quản trị viên")]
public class StaffController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public StaffController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    /// <summary>
    /// Tạo tài khoản nhân viên mới
    /// </summary>
    /// <remarks>
    /// Mẫu request:
    /// 
    ///     POST /api/cms/staff
    ///     {
    ///        "firstName": "Nguyễn",
    ///        "lastName": "Văn A",
    ///        "email": "staff@booklify.com",
    ///        "phone": "0123456789",
    ///        "address": "123 Đường ABC, Quận XYZ, TP. HCM",
    ///        "password": "Staff@123",
    ///        "position": 1
    ///     }
    ///     
    /// Các giá trị position:
    /// - 0: Unknown
    /// - 1: Administrator
    /// - 2: Staff
    /// - 3: UserManager
    /// - 4: LibraryManager
    /// </remarks>
    /// <param name="request">Thông tin nhân viên mới</param>
    /// <returns>Thông tin nhân viên đã tạo</returns>
    /// <response code="200">Tạo tài khoản nhân viên thành công</response>
    /// <response code="400">Dữ liệu không hợp lệ</response>
    /// <response code="401">Không có quyền truy cập</response>
    /// <response code="403">Không đủ quyền hạn (yêu cầu quyền Admin)</response>
    [HttpPost]
    [ProducesResponseType(typeof(Result<CreatedStaffResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 403)]
    [SwaggerOperation(
        Summary = "Tạo tài khoản nhân viên mới",
        Description = "API tạo tài khoản nhân viên dành cho quản trị viên",
        OperationId = "Admin_CreateStaff",
        Tags = new[] { "Admin", "Admin_Staff" }
    )]
    public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest request)
    {
        var command = new CreateStaffCommand(request);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }
} 