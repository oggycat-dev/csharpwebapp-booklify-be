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

public class ApprovalStatusDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Query to get list of approval statuses for dropdown
/// </summary>
public record GetApprovalStatusesQuery : IRequest<Result<List<ApprovalStatusDto>>>;

/// <summary>
/// Handler for GetApprovalStatusesQuery
/// </summary>
public class GetApprovalStatusesQueryHandler : IRequestHandler<GetApprovalStatusesQuery, Result<List<ApprovalStatusDto>>>
{
    public async Task<Result<List<ApprovalStatusDto>>> Handle(GetApprovalStatusesQuery request, CancellationToken cancellationToken)
    {
        // Get statuses from enum
        var statuses = Enum.GetValues(typeof(ApprovalStatus))
            .Cast<ApprovalStatus>()
            .Select(s => new ApprovalStatusDto
            {
                Id = (int)s,
                Name = s.ToString(),
                Description = GetApprovalStatusDescription(s)
            })
            .OrderBy(s => s.Id)
            .ToList();
            
        // Ensure this is async to match the interface
        await Task.CompletedTask;
        
        return Result<List<ApprovalStatusDto>>.Success(statuses);
    }
    
    private string GetApprovalStatusDescription(ApprovalStatus status)
    {
        return status switch
        {
            ApprovalStatus.Pending => "Chờ duyệt",
            ApprovalStatus.Approved => "Đã duyệt",
            ApprovalStatus.Rejected => "Từ chối",

            _ => string.Empty
        };
    }
}
