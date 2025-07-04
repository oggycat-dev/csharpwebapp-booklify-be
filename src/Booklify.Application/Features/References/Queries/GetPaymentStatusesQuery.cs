using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;

namespace Booklify.Application.Features.References.Queries;

public class PaymentStatusDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Query to get list of payment statuses for dropdown
/// </summary>
public record GetPaymentStatusesQuery : IRequest<Result<List<PaymentStatusDto>>>;

/// <summary>
/// Handler for GetPaymentStatusesQuery
/// </summary>
public class GetPaymentStatusesQueryHandler : IRequestHandler<GetPaymentStatusesQuery, Result<List<PaymentStatusDto>>>
{
    public async Task<Result<List<PaymentStatusDto>>> Handle(GetPaymentStatusesQuery request, CancellationToken cancellationToken)
    {
        // Get statuses from enum
        var statuses = Enum.GetValues(typeof(PaymentStatus))
            .Cast<PaymentStatus>()
            .Select(s => new PaymentStatusDto
            {
                Id = (int)s,
                Name = s.ToString(),
                Description = GetPaymentStatusDescription(s)
            })
            .OrderBy(s => s.Id)
            .ToList();
            
        // Ensure this is async to match the interface
        await Task.CompletedTask;
        
        return Result<List<PaymentStatusDto>>.Success(statuses);
    }
    
    private string GetPaymentStatusDescription(PaymentStatus status)
    {
        return status switch
        {
            PaymentStatus.Pending => "Chờ thanh toán",
            PaymentStatus.Success => "Thành công",
            PaymentStatus.Failed => "Thất bại",
            PaymentStatus.Cancelled => "Đã hủy",
            PaymentStatus.Refunded => "Đã hoàn tiền",
            PaymentStatus.Processing => "Đang xử lý",
            _ => string.Empty
        };
    }
}
