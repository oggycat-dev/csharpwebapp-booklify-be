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

public class RoleDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Query to get list of roles for dropdown
/// </summary>
public record GetRolesQuery : IRequest<Result<List<RoleDto>>>;

/// <summary>
/// Handler for GetRolesQuery
/// </summary>
public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, Result<List<RoleDto>>>
{
    public async Task<Result<List<RoleDto>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        // Get roles from enum
        var roles = Enum.GetValues(typeof(Role))
            .Cast<Role>()
            .Select(r => new RoleDto
            {
                Id = (int)r,
                Name = r.ToString(),
                Description = GetRoleDescription(r)
            })
            .OrderBy(r => r.Id)
            .ToList();
            
        // Ensure this is async to match the interface
        await Task.CompletedTask;
        
        return Result<List<RoleDto>>.Success(roles);
    }
    
    private string GetRoleDescription(Role role)
    {
        return role switch
        {
            Role.User => "Người dùng",
            Role.Staff => "Nhân viên",
            Role.Admin => "Quản trị viên",
            _ => string.Empty
        };
    }
}
