using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Booklify.API.Configurations;
using Booklify.Application.Features.Subscription.Commands.Subscribe;
using Booklify.Application.Features.Subscription.Queries.GetSubscriptions;
using Booklify.Application.Features.Subscription.Queries.GetMySubscriptions;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.DTOs.Subscription;
using Swashbuckle.AspNetCore.Annotations;

namespace Booklify.API.Controllers;

/// <summary>
/// Controller quản lý subscription cho người dùng
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(GroupName = "v1")]
[Configurations.Tags("User", "Subscription")]
[SwaggerTag("API quản lý subscription dành cho người dùng")]
public class SubscriptionController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public SubscriptionController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    /// <summary>
    /// Lấy danh sách các gói subscription
    /// </summary>
    /// <remarks>
    /// API này trả về danh sách tất cả các gói subscription đang hoạt động.
    /// Người dùng có thể xem thông tin chi tiết về giá, thời hạn của từng gói.
    /// </remarks>
    /// <returns>Danh sách gói subscription</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    [HttpGet("plans")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<List<SubscriptionResponse>>), 200)]
    [SwaggerOperation(
        Summary = "Lấy danh sách gói subscription",
        Description = "Lấy tất cả gói subscription đang hoạt động",
        OperationId = "GetSubscriptionPlans",
        Tags = new[] { "User", "Subscription" }
    )]
    public async Task<IActionResult> GetSubscriptionPlans()
    {
        var query = new GetSubscriptionsQuery();
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }
    
    /// <summary>
    /// Đăng ký gói subscription
    /// </summary>
    /// <remarks>
    /// Đăng ký một gói subscription và tạo đơn hàng thanh toán.
    /// Sau khi đăng ký thành công, hệ thống sẽ trả về URL thanh toán VNPay.
    /// 
    /// Mẫu request:
    /// 
    ///     POST /api/subscription/subscribe
    ///     {
    ///        "subscription_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///        "auto_renew": true,
    ///        "payment_method": "VNPay"
    ///     }
    /// </remarks>
    /// <param name="request">Thông tin đăng ký</param>
    /// <returns>Thông tin đăng ký và URL thanh toán</returns>
    /// <response code="200">Đăng ký thành công</response>
    /// <response code="400">Lỗi validation hoặc user đã có subscription</response>
    /// <response code="401">Chưa đăng nhập</response>
    /// <response code="404">Không tìm thấy gói subscription</response>
    [HttpPost("subscribe")]
    [Authorize]
    [ProducesResponseType(typeof(Result<SubscribeResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 401)]
    [ProducesResponseType(typeof(Result), 404)]
    [SwaggerOperation(
        Summary = "Đăng ký gói subscription",
        Description = "Tạo subscription và đơn hàng thanh toán",
        OperationId = "SubscribeToPackage",
        Tags = new[] { "User", "Subscription" }
    )]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
    {
        var command = new SubscribeCommand(request);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }
    
    /// <summary>
    /// Lấy danh sách subscription của tôi
    /// </summary>
    /// <remarks>
    /// Lấy danh sách tất cả subscription mà người dùng hiện tại đã đăng ký,
    /// bao gồm cả subscription đang hoạt động và đã hết hạn.
    /// </remarks>
    /// <returns>Danh sách subscription của user</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    /// <response code="401">Chưa đăng nhập</response>
    [HttpGet("my-subscriptions")]
    [Authorize]
    [ProducesResponseType(typeof(Result<List<UserSubscriptionResponse>>), 200)]
    [ProducesResponseType(typeof(Result), 401)]
    [SwaggerOperation(
        Summary = "Lấy subscription của tôi",
        Description = "Lấy danh sách subscription của user hiện tại",
        OperationId = "GetMySubscriptions",
        Tags = new[] { "User", "Subscription" }
    )]
    public async Task<IActionResult> GetMySubscriptions()
    {
        var query = new GetMySubscriptionsQuery();
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return StatusCode(result.GetHttpStatusCode(), result);
            
        return Ok(result);
    }
} 