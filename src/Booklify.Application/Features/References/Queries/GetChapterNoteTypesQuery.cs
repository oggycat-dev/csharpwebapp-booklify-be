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

public class ChapterNoteTypeDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Query to get list of chapter note types for dropdown
/// </summary>
public record GetChapterNoteTypesQuery : IRequest<Result<List<ChapterNoteTypeDto>>>;

/// <summary>
/// Handler for GetChapterNoteTypesQuery
/// </summary>
public class GetChapterNoteTypesQueryHandler : IRequestHandler<GetChapterNoteTypesQuery, Result<List<ChapterNoteTypeDto>>>
{
    public async Task<Result<List<ChapterNoteTypeDto>>> Handle(GetChapterNoteTypesQuery request, CancellationToken cancellationToken)
    {
        // Get types from enum
        var types = Enum.GetValues(typeof(ChapterNoteType))
            .Cast<ChapterNoteType>()
            .Select(t => new ChapterNoteTypeDto
            {
                Id = (int)t,
                Name = t.ToString(),
                Description = GetChapterNoteTypeDescription(t)
            })
            .OrderBy(t => t.Id)
            .ToList();
            
        // Ensure this is async to match the interface
        await Task.CompletedTask;
        
        return Result<List<ChapterNoteTypeDto>>.Success(types);
    }
    
    private string GetChapterNoteTypeDescription(ChapterNoteType type)
    {
        return type switch
        {
            ChapterNoteType.TextNote => "Ghi chú văn bản",
            ChapterNoteType.Highlight => "Đánh dấu văn bản",
            _ => string.Empty
        };
    }
}
