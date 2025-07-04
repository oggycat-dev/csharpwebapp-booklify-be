using Microsoft.AspNetCore.Mvc;
using Booklify.API.Configurations;
using Booklify.Application.Common.Models;
using Booklify.Application.Features.References.Queries;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Booklify.API.Controllers.Common;

[ApiController]
[Route("api/common/reference")]
[ApiExplorerSettings(GroupName = "v1")]
[Booklify.API.Configurations.Tags("Common", "Common_Reference")]
[SwaggerTag("API dữ liệu tham chiếu cho ứng dụng")]
public class ReferenceDataController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public ReferenceDataController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    /// <summary>
    /// Lấy danh sách trạng thái phê duyệt
    /// </summary>
    /// <returns>Danh sách trạng thái phê duyệt</returns>
    [HttpGet("approval-statuses")]
    [ProducesResponseType(typeof(Result<List<ApprovalStatusDto>>), 200)]
    [SwaggerOperation(
        Summary = "Lấy danh sách trạng thái phê duyệt",
        Description = "Trả về danh sách trạng thái phê duyệt để hiển thị trong dropdown",
        OperationId = "Common_GetApprovalStatuses",
        Tags = new[] { "Common", "Common_Reference" }
    )]
    public async Task<IActionResult> GetApprovalStatuses()
    {
        var result = await _mediator.Send(new GetApprovalStatusesQuery());
        return Ok(result);
    }
    
    /// <summary>
    /// Lấy danh sách trạng thái thực thể
    /// </summary>
    /// <returns>Danh sách trạng thái thực thể</returns>
    [HttpGet("entity-statuses")]
    [ProducesResponseType(typeof(Result<List<EntityStatusDto>>), 200)]
    [SwaggerOperation(
        Summary = "Lấy danh sách trạng thái thực thể",
        Description = "Trả về danh sách trạng thái thực thể để hiển thị trong dropdown",
        OperationId = "Common_GetEntityStatuses",
        Tags = new[] { "Common", "Common_Reference" }
    )]
    public async Task<IActionResult> GetEntityStatuses()
    {
        var result = await _mediator.Send(new GetEntityStatusesQuery());
        return Ok(result);
    }
    
    /// <summary>
    /// Lấy danh sách giới tính
    /// </summary>
    /// <returns>Danh sách giới tính</returns>
    [HttpGet("genders")]
    [ProducesResponseType(typeof(Result<List<GenderDto>>), 200)]
    [SwaggerOperation(
        Summary = "Lấy danh sách giới tính",
        Description = "Trả về danh sách giới tính để hiển thị trong dropdown",
        OperationId = "Common_GetGenders",
        Tags = new[] { "Common", "Common_Reference" }
    )]
    public async Task<IActionResult> GetGenders()
    {
        var result = await _mediator.Send(new GetGendersQuery());
        return Ok(result);
    }
    
    /// <summary>
    /// Lấy danh sách vai trò
    /// </summary>
    /// <returns>Danh sách vai trò</returns>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(Result<List<RoleDto>>), 200)]
    [SwaggerOperation(
        Summary = "Lấy danh sách vai trò",
        Description = "Trả về danh sách vai trò để hiển thị trong dropdown",
        OperationId = "Common_GetRoles",
        Tags = new[] { "Common", "Common_Reference" }
    )]
    public async Task<IActionResult> GetRoles()
    {
        var result = await _mediator.Send(new GetRolesQuery());
        return Ok(result);
    }
    
    /// <summary>
    /// Lấy danh sách vị trí nhân viên
    /// </summary>
    /// <returns>Danh sách vị trí nhân viên</returns>
    [HttpGet("staff-positions")]
    [ProducesResponseType(typeof(Result<List<StaffPositionDto>>), 200)]
    [SwaggerOperation(
        Summary = "Lấy danh sách vị trí nhân viên",
        Description = "Trả về danh sách vị trí nhân viên để hiển thị trong dropdown",
        OperationId = "Common_GetStaffPositions",
        Tags = new[] { "Common", "Common_Reference" }
    )]
    public async Task<IActionResult> GetStaffPositions()
    {
        var result = await _mediator.Send(new GetStaffPositionsQuery());
        return Ok(result);
    }
    
    /// <summary>
    /// Lấy danh sách trạng thái thanh toán
    /// </summary>
    /// <returns>Danh sách trạng thái thanh toán</returns>
    [HttpGet("payment-statuses")]
    [ProducesResponseType(typeof(Result<List<PaymentStatusDto>>), 200)]
    [SwaggerOperation(
        Summary = "Lấy danh sách trạng thái thanh toán",
        Description = "Trả về danh sách trạng thái thanh toán để hiển thị trong dropdown",
        OperationId = "Common_GetPaymentStatuses",
        Tags = new[] { "Common", "Common_Reference" }
    )]
    public async Task<IActionResult> GetPaymentStatuses()
    {
        var result = await _mediator.Send(new GetPaymentStatusesQuery());
        return Ok(result);
    }
    
    /// <summary>
    /// Lấy danh sách loại ghi chú chương
    /// </summary>
    /// <returns>Danh sách loại ghi chú chương</returns>
    [HttpGet("chapter-note-types")]
    [ProducesResponseType(typeof(Result<List<ChapterNoteTypeDto>>), 200)]
    [SwaggerOperation(
        Summary = "Lấy danh sách loại ghi chú chương",
        Description = "Trả về danh sách loại ghi chú chương để hiển thị trong dropdown",
        OperationId = "Common_GetChapterNoteTypes",
        Tags = new[] { "Common", "Common_Reference" }
    )]
    public async Task<IActionResult> GetChapterNoteTypes()
    {
        var result = await _mediator.Send(new GetChapterNoteTypesQuery());
        return Ok(result);
    }
    
    /// <summary>
    /// Lấy danh sách loại file upload
    /// </summary>
    /// <returns>Danh sách loại file upload</returns>
    [HttpGet("file-upload-types")]
    [ProducesResponseType(typeof(Result<List<FileUploadTypeDto>>), 200)]
    [SwaggerOperation(
        Summary = "Lấy danh sách loại file upload",
        Description = "Trả về danh sách loại file upload để hiển thị trong dropdown",
        OperationId = "Common_GetFileUploadTypes",
        Tags = new[] { "Common", "Common_Reference" }
    )]
    public async Task<IActionResult> GetFileUploadTypes()
    {
        var result = await _mediator.Send(new GetFileUploadTypesQuery());
        return Ok(result);
    }
    
    /// <summary>
    /// Lấy danh sách trạng thái job file
    /// </summary>
    /// <returns>Danh sách trạng thái job file</returns>
    [HttpGet("file-job-statuses")]
    [ProducesResponseType(typeof(Result<List<FileJobStatusDto>>), 200)]
    [SwaggerOperation(
        Summary = "Lấy danh sách trạng thái job file",
        Description = "Trả về danh sách trạng thái job file để hiển thị trong dropdown",
        OperationId = "Common_GetFileJobStatuses",
        Tags = new[] { "Common", "Common_Reference" }
    )]
    public async Task<IActionResult> GetFileJobStatuses()
    {
        var result = await _mediator.Send(new GetFileJobStatusesQuery());
        return Ok(result);
    }
    
    /// <summary>
    /// Lấy danh sách danh mục sách
    /// </summary>
    /// <returns>Danh sách danh mục sách</returns>
    [HttpGet("book-categories")]
    [ProducesResponseType(typeof(Result<List<BookCategoryDto>>), 200)]
    [SwaggerOperation(
        Summary = "Lấy danh sách danh mục sách",
        Description = "Trả về danh sách danh mục sách đang hoạt động để hiển thị trong dropdown",
        OperationId = "Common_GetBookCategories",
        Tags = new[] { "Common", "Common_Reference" }
    )]
    public async Task<IActionResult> GetBookCategories()
    {
        var result = await _mediator.Send(new GetBookCategoriesQuery());
        return Ok(result);
    }
}
