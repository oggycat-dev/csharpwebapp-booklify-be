using MediatR;
using Booklify.Application.Common.DTOs.Staff;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Staff.Queries.GetStaffs;

/// <summary>
/// Query to get a list of staffs with filtering and pagination
/// </summary>
public record GetStaffsQuery(StaffFilterModel? Filter) : IRequest<PaginatedResult<StaffResponse>>; 