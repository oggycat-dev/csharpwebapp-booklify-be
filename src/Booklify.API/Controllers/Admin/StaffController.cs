using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Booklify.API.Attributes;
using Booklify.API.Configurations;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.DTOs.Staff;
using Booklify.Application.Features.Staff.Commands.CreateStaff;
using Booklify.Application.Features.Staff.Commands.UpdateStaff;
using Booklify.Application.Features.Staff.Queries.GetStaffs;
using Booklify.Application.Features.Staff.Queries.GetStaffById;
using Booklify.Domain.Enums;
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
    /// Lấy danh sách nhân viên với tùy chọn lọc, sắp xếp và phân trang
    /// </summary>
    /// <remarks>
    /// API này hỗ trợ lọc, sắp xếp và phân trang cho danh sách nhân viên.
    /// 
    /// Các tham số lọc:
    /// - staffCode: Lọc theo mã nhân viên (tìm kiếm gần đúng)
    /// - fullName: Lọc theo tên nhân viên (tìm kiếm gần đúng)
    /// - email: Lọc theo email (tìm kiếm gần đúng)
    /// - phone: Lọc theo số điện thoại (tìm kiếm gần đúng)
    /// - position: Lọc theo vị trí công việc (0: Unknown, 1: Administrator, 2: Staff, 3: UserManager, 4: LibraryManager)
    /// - isActive: Lọc theo trạng thái tài khoản (true: Hoạt động, false: Không hoạt động)
    /// 
    /// Các tham số sắp xếp:
    /// - sortBy: Trường dữ liệu dùng để sắp xếp (staffcode, fullname, email, phone, position, createdat)
    /// - isAscending: Sắp xếp tăng dần (true) hoặc giảm dần (false)
    /// 
    /// Các tham số phân trang:
    /// - pageNumber: Số trang (mặc định: 1)
    /// - pageSize: Số lượng bản ghi trên một trang (mặc định: 10)
    /// </remarks>
    [HttpGet("list")]
    [ProducesResponseType(typeof(PaginatedResult<StaffResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 403)]
    [ProducesResponseType(typeof(Result), 500)]
    [SwaggerOperation(
        Summary = "Lấy danh sách nhân viên",
        Description = "Lấy danh sách nhân viên với tùy chọn lọc, sắp xếp và phân trang. Chỉ Admin mới có quyền truy cập.",
        OperationId = "Admin_GetStaffs",
        Tags = new[] { "Admin", "Admin_Staff" }
    )]
    public async Task<IActionResult> GetStaffs(
        [FromQuery] string? staffCode,
        [FromQuery] string? fullName,
        [FromQuery] string? email,
        [FromQuery] string? phone,
        [FromQuery] StaffPosition? position,
        [FromQuery] bool? isActive,
        [FromQuery] string? sortBy,
        [FromQuery] bool isAscending = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var filter = new StaffFilterModel(pageNumber, pageSize)
        {
            StaffCode = staffCode,
            FullName = fullName,
            Email = email,
            Phone = phone,
            Position = position,
            IsActive = isActive,
            SortBy = sortBy,
            IsAscending = isAscending
        };
        
        var result = await _mediator.Send(new GetStaffsQuery(filter));
        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }
        return Ok(result);
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
    
    /// <summary>
    /// Cập nhật thông tin nhân viên (PATCH - Partial Update)
    /// </summary>
    /// <remarks>
    /// API này hỗ trợ cập nhật một phần thông tin nhân viên. Chỉ các trường được gửi lên mới được cập nhật.
    /// 
    /// Mẫu request (chỉ cần gửi các trường muốn cập nhật):
    /// 
    ///     PATCH /api/cms/staff/{id}
    ///     {
    ///        "firstName": "Tên mới",
    ///        "phone": "0987654321",
    ///        "isActive": false
    ///     }
    ///     
    /// Các trường có thể cập nhật:
    /// - firstName: Tên
    /// - lastName: Họ
    /// - phone: Số điện thoại
    /// - email: Email
    /// - address: Địa chỉ
    /// - position: Vị trí (0-4)
    /// - isActive: Trạng thái hoạt động (true/false)
    ///     
    /// Lưu ý: Validation sẽ bị skip cho các trường không được gửi lên.
    /// </remarks>
    /// <param name="id">ID của nhân viên cần cập nhật</param>
    /// <param name="request">Thông tin cập nhật (chỉ chứa các trường muốn thay đổi)</param>
    /// <returns>Thông tin nhân viên đã cập nhật</returns>
    /// <response code="200">Cập nhật thành công</response>
    /// <response code="400">Dữ liệu không hợp lệ hoặc không có thay đổi</response>
    /// <response code="401">Không có quyền truy cập</response>
    /// <response code="403">Không đủ quyền hạn (yêu cầu quyền Admin)</response>
    /// <response code="404">Không tìm thấy nhân viên</response>
    [HttpPatch("{id}")]
    [SkipModelValidation] // Skip model validation for partial updates
    [ProducesResponseType(typeof(Result<StaffResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 403)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Cập nhật thông tin nhân viên (Partial Update)",
        Description = "API cập nhật một phần thông tin nhân viên. Model validation sẽ bị skip, chỉ validate các trường được gửi lên.",
        OperationId = "Admin_UpdateStaff",
        Tags = new[] { "Admin", "Admin_Staff" }
    )]
    public async Task<IActionResult> UpdateStaff(
        [FromRoute] Guid id,
        [FromBody] UpdateStaffRequest request)
    {
        var command = new UpdateStaffCommand(id, request);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một nhân viên theo ID
    /// </summary>
    /// <param name="id">ID của nhân viên cần xem thông tin</param>
    /// <returns>Thông tin chi tiết của nhân viên</returns>
    /// <response code="200">Lấy thông tin thành công</response>
    /// <response code="401">Không có quyền truy cập</response>
    /// <response code="403">Không đủ quyền hạn (yêu cầu quyền Admin)</response>
    /// <response code="404">Không tìm thấy nhân viên</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<StaffResponse>), 200)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 403)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Lấy thông tin chi tiết của một nhân viên",
        Description = "API lấy thông tin chi tiết của một nhân viên theo ID. Chỉ Admin mới có quyền truy cập.",
        OperationId = "Admin_GetStaffById",
        Tags = new[] { "Admin", "Admin_Staff" }
    )]
    public async Task<IActionResult> GetStaffById([FromRoute] Guid id)
    {
        var query = new GetStaffByIdQuery(id);
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }
} 