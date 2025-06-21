using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Booklify.API.Configurations;
using Booklify.API.Middlewares;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Models;
using Booklify.Application.Features.Subscription.Commands.CreateSubscription;
using Booklify.Application.Features.Subscription.Commands.UpdateSubscription;
using Booklify.Application.Features.Subscription.Commands.DeleteSubscription;
using Booklify.Application.Features.Subscription.Queries.GetAllSubscriptions;
using Booklify.Application.Features.Subscription.Queries.GetSubscriptionById;
using Booklify.Domain.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace Booklify.API.Controllers.Admin;

/// <summary>
/// Admin controller for managing subscription plans
/// </summary>
[ApiController]
[Route("api/admin/[controller]")]
[ApiExplorerSettings(GroupName = "admin")]
[Authorize]
[AuthorizeRoles("Admin", "Staff")]
[Configurations.Tags("Admin", "Subscription Management")]
[SwaggerTag("Admin APIs cho quản lý subscription plans")]
public class SubscriptionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(IMediator mediator, ILogger<SubscriptionController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all subscription plans with filtering and pagination
    /// </summary>
    /// <param name="filter">Filter parameters</param>
    /// <returns>Paginated list of subscription plans</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Result<PaginatedResult<SubscriptionResponse>>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [SwaggerOperation(
        Summary = "Get all subscription plans",
        Description = "Lấy danh sách tất cả subscription plans với filtering và pagination",
        OperationId = "AdminGetAllSubscriptions",
        Tags = new[] { "Admin", "Subscription Management" }
    )]
    public async Task<IActionResult> GetAllSubscriptions([FromQuery] SubscriptionFilterModel filter)
    {
        try
        {
            var query = new GetAllSubscriptionsQuery { Filter = filter };
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription plans");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get subscription plan by ID
    /// </summary>
    /// <param name="id">Subscription plan ID</param>
    /// <returns>Subscription plan details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<SubscriptionResponse>), 200)]
    [ProducesResponseType(typeof(Result), 404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [SwaggerOperation(
        Summary = "Get subscription plan by ID",
        Description = "Lấy chi tiết subscription plan theo ID",
        OperationId = "AdminGetSubscriptionById",
        Tags = new[] { "Admin", "Subscription Management" }
    )]
    public async Task<IActionResult> GetSubscriptionById(Guid id)
    {
        try
        {
            var query = new GetSubscriptionByIdQuery(id);
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return NotFound(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription plan by ID: {SubscriptionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a new subscription plan
    /// </summary>
    /// <param name="request">Subscription plan creation data</param>
    /// <returns>Created subscription plan</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Result<SubscriptionResponse>), 201)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [SwaggerOperation(
        Summary = "Create subscription plan",
        Description = "Tạo subscription plan mới",
        OperationId = "AdminCreateSubscription",
        Tags = new[] { "Admin", "Subscription Management" }
    )]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var command = new CreateSubscriptionCommand { Request = request };
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Subscription plan created successfully: {SubscriptionName}", request.Name);
                return CreatedAtAction(nameof(GetSubscriptionById), new { id = result.Data?.Id }, result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription plan: {SubscriptionName}", request.Name);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update an existing subscription plan
    /// </summary>
    /// <param name="id">Subscription plan ID</param>
    /// <param name="request">Subscription plan update data</param>
    /// <returns>Updated subscription plan</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Result<SubscriptionResponse>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [SwaggerOperation(
        Summary = "Update subscription plan",
        Description = "Cập nhật subscription plan",
        OperationId = "AdminUpdateSubscription",
        Tags = new[] { "Admin", "Subscription Management" }
    )]
    public async Task<IActionResult> UpdateSubscription(Guid id, [FromBody] UpdateSubscriptionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var command = new UpdateSubscriptionCommand { Id = id, Request = request };
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Subscription plan updated successfully: {SubscriptionId}", id);
                return Ok(result);
            }

            return result.ErrorCode == ErrorCode.NotFound ? NotFound(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription plan: {SubscriptionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a subscription plan (soft delete)
    /// </summary>
    /// <param name="id">Subscription plan ID</param>
    /// <returns>Result of delete operation</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 404)]
    [ProducesResponseType(typeof(Result), 422)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [SwaggerOperation(
        Summary = "Delete subscription plan",
        Description = "Xóa subscription plan (soft delete). Không thể xóa nếu có user đang sử dụng.",
        OperationId = "AdminDeleteSubscription",
        Tags = new[] { "Admin", "Subscription Management" }
    )]
    public async Task<IActionResult> DeleteSubscription(Guid id)
    {
        try
        {
            var command = new DeleteSubscriptionCommand(id);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Subscription plan deleted successfully: {SubscriptionId}", id);
                return Ok(result);
            }

            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result),
                ErrorCode.BusinessRuleViolation => UnprocessableEntity(result),
                _ => BadRequest(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subscription plan: {SubscriptionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get subscription statistics
    /// </summary>
    /// <returns>Subscription statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [SwaggerOperation(
        Summary = "Get subscription statistics",
        Description = "Lấy thống kê về subscription plans",
        OperationId = "AdminGetSubscriptionStatistics",
        Tags = new[] { "Admin", "Subscription Management" }
    )]
    public async Task<IActionResult> GetSubscriptionStatistics()
    {
        try
        {
            // This could be implemented as a separate query
            // For now, return a placeholder
            var stats = new
            {
                TotalPlans = 0,
                ActiveSubscribers = 0,
                Revenue = 0m,
                PopularPlan = "N/A"
            };

            return Ok(new { result = "success", data = stats });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
} 