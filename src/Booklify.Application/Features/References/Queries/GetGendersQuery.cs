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

public class GenderDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Query to get list of genders for dropdown
/// </summary>
public record GetGendersQuery : IRequest<Result<List<GenderDto>>>;

/// <summary>
/// Handler for GetGendersQuery
/// </summary>
public class GetGendersQueryHandler : IRequestHandler<GetGendersQuery, Result<List<GenderDto>>>
{
    public async Task<Result<List<GenderDto>>> Handle(GetGendersQuery request, CancellationToken cancellationToken)
    {
        // Get genders from enum
        var genders = Enum.GetValues(typeof(Gender))
            .Cast<Gender>()
            .Select(g => new GenderDto
            {
                Id = (int)g,
                Name = g.ToString(),
                Description = GetGenderDescription(g)
            })
            .OrderBy(g => g.Id)
            .ToList();
            
        // Ensure this is async to match the interface
        await Task.CompletedTask;
        
        return Result<List<GenderDto>>.Success(genders);
    }
    
    private string GetGenderDescription(Gender gender)
    {
        return gender switch
        {
            Gender.Female => "Nữ",
            Gender.Male => "Nam",
            Gender.Other => "Khác",
            _ => string.Empty
        };
    }
}
