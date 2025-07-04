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

public class FileJobStatusDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Query to get list of file job statuses for dropdown
/// </summary>
public record GetFileJobStatusesQuery : IRequest<Result<List<FileJobStatusDto>>>;

/// <summary>
/// Handler for GetFileJobStatusesQuery
/// </summary>
public class GetFileJobStatusesQueryHandler : IRequestHandler<GetFileJobStatusesQuery, Result<List<FileJobStatusDto>>>
{
    public async Task<Result<List<FileJobStatusDto>>> Handle(GetFileJobStatusesQuery request, CancellationToken cancellationToken)
    {
        // Get statuses from enum
        var statuses = Enum.GetValues(typeof(FileJobStatus))
            .Cast<FileJobStatus>()
            .Select(s => new FileJobStatusDto
            {
                Id = (int)s,
                Name = s.ToString(),
                Description = GetFileJobStatusDescription(s)
            })
            .OrderBy(s => s.Id)
            .ToList();
            
        // Ensure this is async to match the interface
        await Task.CompletedTask;
        
        return Result<List<FileJobStatusDto>>.Success(statuses);
    }
    
    private string GetFileJobStatusDescription(FileJobStatus status)
    {
        return status switch
        {
            FileJobStatus.None => "Không xác định",
            FileJobStatus.Pending => "Chờ xử lý",
            FileJobStatus.Processing => "Đang xử lý",
            FileJobStatus.Completed => "Hoàn thành",
            FileJobStatus.Failed => "Thất bại",
            FileJobStatus.Cancelled => "Đã hủy",
            _ => string.Empty
        };
    }
}
