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

public class StaffPositionDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Query to get list of staff positions for dropdown
/// </summary>
public record GetStaffPositionsQuery : IRequest<Result<List<StaffPositionDto>>>;

/// <summary>
/// Handler for GetStaffPositionsQuery
/// </summary>
public class GetStaffPositionsQueryHandler : IRequestHandler<GetStaffPositionsQuery, Result<List<StaffPositionDto>>>
{
    public async Task<Result<List<StaffPositionDto>>> Handle(GetStaffPositionsQuery request, CancellationToken cancellationToken)
    {
        // Get positions from enum
        var positions = Enum.GetValues(typeof(StaffPosition))
            .Cast<StaffPosition>()
            .Select(p => new StaffPositionDto
            {
                Id = (int)p,
                Name = p.ToString(),
                Description = GetStaffPositionDescription(p)
            })
            .OrderBy(p => p.Id)
            .ToList();
            
        // Ensure this is async to match the interface
        await Task.CompletedTask;
        
        return Result<List<StaffPositionDto>>.Success(positions);
    }
    
    private string GetStaffPositionDescription(StaffPosition position)
    {
        return position switch
        {
            StaffPosition.Unknown => "Không xác định",
            StaffPosition.Administrator => "Quản trị hệ thống",
            StaffPosition.Staff => "Nhân viên quản lý nội dung",
            StaffPosition.UserManager => "Quản lý tài khoản người dùng",
            StaffPosition.LibraryManager => "Quản lý thư viện",
            StaffPosition.TechnicalSupport => "Hỗ trợ kỹ thuật",
            StaffPosition.DataEntryClerk => "Nhân viên nhập liệu",
            StaffPosition.CommunityModerator => "Quản lý cộng đồng",
            StaffPosition.AIAssistantManager => "Quản lý AI/ML",
            _ => string.Empty
        };
    }
}
