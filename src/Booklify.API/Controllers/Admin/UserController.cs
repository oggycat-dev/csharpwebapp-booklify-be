using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Booklify.API.Configurations;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.DTOs.User;
using Booklify.Application.Features.User.Queries.GetUsers;
using Booklify.Application.Features.User.Queries.GetUserById;
using Booklify.Application.Features.User.Commands.UpdateUserStatus;
using Booklify.Domain.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace Booklify.API.Controllers.Admin;

/// <summary>
/// Controller quản lý người dùng cho ứng dụng quản trị viên
/// </summary>
[ApiController]
[Route("api/cms/users")]
[ApiExplorerSettings(GroupName = "v1")]
[Authorize(Roles = "Admin")]
[Configurations.Tags("Admin", "Admin_Users")]
[SwaggerTag("API quản lý người dùng dành cho quản trị viên")]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy danh sách người dùng với tùy chọn lọc, sắp xếp và phân trang
    /// </summary>
    /// <remarks>
    /// API này hỗ trợ lọc, sắp xếp và phân trang cho danh sách người dùng.
    /// 
    /// Các tham số lọc:
    /// - searchKeyword: Tìm kiếm trong tên, username, email (tìm kiếm gần đúng)
    /// - gender: Lọc theo giới tính (0: Unknown, 1: Male, 2: Female, 3: Other)
    /// - isActive: Lọc theo trạng thái tài khoản (true: Hoạt động, false: Không hoạt động)
    /// 
    /// Các tham số sắp xếp:
    /// - sortBy: Trường dữ liệu dùng để sắp xếp (name, firstname, email, username, gender, createdat)
    /// - isAscending: Sắp xếp tăng dần (true) hoặc giảm dần (false)
    /// 
    /// Các tham số phân trang:
    /// - pageNumber: Số trang (mặc định: 1)
    /// - pageSize: Số lượng bản ghi trên một trang (mặc định: 10)
    /// </remarks>
    [HttpGet("list")]
    [ProducesResponseType(typeof(PaginatedResult<UserResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 403)]
    [ProducesResponseType(typeof(Result), 500)]
    [SwaggerOperation(
        Summary = "Lấy danh sách người dùng",
        Description = "Lấy danh sách người dùng với tùy chọn lọc, sắp xếp và phân trang. Chỉ Admin mới có quyền truy cập.",
        OperationId = "Admin_GetUsers",
        Tags = new[] { "Admin", "Admin_Users" }
    )]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? searchKeyword,
        [FromQuery] Gender? gender,
        [FromQuery] bool? isActive,
        [FromQuery] string? sortBy,
        [FromQuery] bool isAscending = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var filter = new UserFilterModel(pageNumber, pageSize)
        {
            SearchKeyword = searchKeyword,
            Gender = gender,
            IsActive = isActive,
            SortBy = sortBy,
            IsAscending = isAscending
        };
        
        var result = await _mediator.Send(new GetUsersQuery(filter));
        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một người dùng theo ID
    /// </summary>
    /// <param name="id">ID của người dùng cần xem thông tin</param>
    /// <returns>Thông tin chi tiết của người dùng bao gồm subscription và avatar</returns>
    /// <response code="200">Lấy thông tin thành công</response>
    /// <response code="401">Không có quyền truy cập</response>
    /// <response code="403">Không đủ quyền hạn (yêu cầu quyền Admin)</response>
    /// <response code="404">Không tìm thấy người dùng</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<UserDetailResponse>), 200)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 403)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Lấy thông tin chi tiết của một người dùng",
        Description = "API lấy thông tin chi tiết của một người dùng theo ID bao gồm thông tin subscription và avatar. Chỉ Admin mới có quyền truy cập.",
        OperationId = "Admin_GetUserById",
        Tags = new[] { "Admin", "Admin_Users" }
    )]
    public async Task<IActionResult> GetUserById([FromRoute] Guid id)
    {
        var query = new GetUserByIdQuery(id);
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật trạng thái tài khoản người dùng (kích hoạt/vô hiệu hóa)
    /// </summary>
    /// <remarks>
    /// API này cho phép Admin toggle trạng thái tài khoản của người dùng.
    /// 
    /// Mẫu request:
    /// 
    ///     PATCH /api/cms/users/{id}/status
    ///     {
    ///        "is_active": true
    ///     }
    ///     
    /// - is_active: true để kích hoạt tài khoản, false để vô hiệu hóa
    /// </remarks>
    /// <param name="id">ID của người dùng cần cập nhật trạng thái</param>
    /// <param name="request">Thông tin trạng thái mới</param>
    /// <returns>Thông báo kết quả và trạng thái hiện tại</returns>
    /// <response code="200">Cập nhật trạng thái thành công</response>
    /// <response code="400">Dữ liệu không hợp lệ hoặc trạng thái không thay đổi</response>
    /// <response code="401">Không có quyền truy cập</response>
    /// <response code="403">Không đủ quyền hạn (yêu cầu quyền Admin)</response>
    /// <response code="404">Không tìm thấy người dùng</response>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 403)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Cập nhật trạng thái tài khoản người dùng",
        Description = "API cho phép Admin toggle trạng thái tài khoản của người dùng (kích hoạt/vô hiệu hóa). Chỉ Admin mới có quyền truy cập.",
        OperationId = "Admin_UpdateUserStatus",
        Tags = new[] { "Admin", "Admin_Users" }
    )]
    public async Task<IActionResult> UpdateUserStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateUserStatusRequest request)
    {
        var command = new UpdateUserStatusCommand(id, request);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }
} 