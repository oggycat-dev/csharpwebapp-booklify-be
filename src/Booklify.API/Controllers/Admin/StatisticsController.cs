using Microsoft.AspNetCore.Mvc;
using MediatR;
using Booklify.Application.Features.Statistics;
using Booklify.Application.Common.DTOs.Statistics;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;
using Booklify.Application.Common.Models;

namespace Booklify.API.Controllers.Admin
{
    [ApiController]
    [Route("api/cms/statistics")]
    [ApiExplorerSettings(GroupName = "v1")]
    [Authorize(Roles = "Admin")]
    [SwaggerTag("API thống kê tổng quan dành cho quản trị viên")]
    public class StatisticsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public StatisticsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy thống kê tổng quan hệ thống cho admin
        /// </summary>
        /// <remarks>
        /// Bao gồm tổng người dùng, tổng sách, tổng người dùng premium, tổng lượt đọc sách.
        /// </remarks>
        /// <response code="200">Lấy thống kê thành công</response>
        /// <response code="401">Không có quyền truy cập</response>
        /// <response code="403">Không đủ quyền hạn (yêu cầu quyền Admin)</response>
        [HttpGet]
        [ProducesResponseType(typeof(Result<AdminStatisticsDto>), 200)]
        [ProducesResponseType(typeof(Result), 401)]
        [ProducesResponseType(typeof(Result), 403)]
        [SwaggerOperation(
            Summary = "Lấy thống kê tổng quan hệ thống",
            Description = "Lấy thống kê tổng quan hệ thống cho admin. Chỉ Admin mới có quyền truy cập.",
            OperationId = "Admin_GetStatistics",
            Tags = new[] { "Admin", "Admin_Statistics" }
        )]
        public async Task<IActionResult> GetStatistics()
        {
            var result = await _mediator.Send(new GetAdminStatisticsQuery());
            if (result.IsSuccess)
                return Ok(result);

            return StatusCode(result.GetHttpStatusCode(), result);
        }
    }
} 