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

public class EntityStatusDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Query to get list of entity statuses for dropdown
/// </summary>
public record GetEntityStatusesQuery : IRequest<Result<List<EntityStatusDto>>>;

/// <summary>
/// Handler for GetEntityStatusesQuery
/// </summary>
public class GetEntityStatusesQueryHandler : IRequestHandler<GetEntityStatusesQuery, Result<List<EntityStatusDto>>>
{
    public async Task<Result<List<EntityStatusDto>>> Handle(GetEntityStatusesQuery request, CancellationToken cancellationToken)
    {
        // Get statuses from enum
        var statuses = Enum.GetValues(typeof(EntityStatus))
            .Cast<EntityStatus>()
            .Select(s => new EntityStatusDto
            {
                Id = (int)s,
                Name = s.ToString(),
                Description = GetEntityStatusDescription(s)
            })
            .OrderBy(s => s.Id)
            .ToList();
            
        // Ensure this is async to match the interface
        await Task.CompletedTask;
        
        return Result<List<EntityStatusDto>>.Success(statuses);
    }
    
    private string GetEntityStatusDescription(EntityStatus status)
    {
        return status switch
        {
            EntityStatus.Inactive => "Không hoạt động",
            EntityStatus.Active => "Đang hoạt động",
            _ => string.Empty
        };
    }
}
