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

public class FileUploadTypeDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Query to get list of file upload types for dropdown
/// </summary>
public record GetFileUploadTypesQuery : IRequest<Result<List<FileUploadTypeDto>>>;

/// <summary>
/// Handler for GetFileUploadTypesQuery
/// </summary>
public class GetFileUploadTypesQueryHandler : IRequestHandler<GetFileUploadTypesQuery, Result<List<FileUploadTypeDto>>>
{
    public async Task<Result<List<FileUploadTypeDto>>> Handle(GetFileUploadTypesQuery request, CancellationToken cancellationToken)
    {
        // Get types from enum
        var types = Enum.GetValues(typeof(FileUploadType))
            .Cast<FileUploadType>()
            .Select(t => new FileUploadTypeDto
            {
                Id = (int)t,
                Name = t.ToString(),
                Description = GetFileUploadTypeDescription(t)
            })
            .OrderBy(t => t.Id)
            .ToList();
            
        // Ensure this is async to match the interface
        await Task.CompletedTask;
        
        return Result<List<FileUploadTypeDto>>.Success(types);
    }
    
    private string GetFileUploadTypeDescription(FileUploadType type)
    {
        return type switch
        {
            FileUploadType.None => "Không xác định",
            FileUploadType.Avatar => "Ảnh đại diện",
            FileUploadType.Document => "Tài liệu",
            FileUploadType.Image => "Hình ảnh",
            FileUploadType.Book => "Sách",
            FileUploadType.BookCover => "Bìa sách",
            FileUploadType.Epub => "File EPUB",
            _ => string.Empty
        };
    }
}
