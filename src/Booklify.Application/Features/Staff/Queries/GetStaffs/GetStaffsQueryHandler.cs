using MediatR;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Booklify.Application.Common.DTOs.Staff;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.Interfaces.Repositories;
using Booklify.Domain.Entities;

namespace Booklify.Application.Features.Staff.Queries.GetStaffs;

public class GetStaffsQueryHandler : IRequestHandler<GetStaffsQuery, PaginatedResult<StaffResponse>>
{
    private readonly IStaffProfileRepository _staffRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetStaffsQueryHandler> _logger;
    
    public GetStaffsQueryHandler(
        IStaffProfileRepository staffRepository,
        IMapper mapper,
        ILogger<GetStaffsQueryHandler> logger)
    {
        _staffRepository = staffRepository;
        _mapper = mapper;
        _logger = logger;
    }
    
    public async Task<PaginatedResult<StaffResponse>> Handle(GetStaffsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var filter = request.Filter ?? new StaffFilterModel();
            
            // Get paged staffs from repository
            var (staffs, totalCount) = await _staffRepository.GetPagedStaffsAsync(filter);
            
            // Map to response DTOs using AutoMapper
            var staffResponses = _mapper.Map<List<StaffResponse>>(staffs);
            
            // Return paginated result
            return PaginatedResult<StaffResponse>.Success(
                staffResponses, 
                filter.PageNumber,
                filter.PageSize,
                totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying staff list");
            return PaginatedResult<StaffResponse>.Failure("An error occurred while querying the staff list", ErrorCode.InternalError);
        }
    }
} 