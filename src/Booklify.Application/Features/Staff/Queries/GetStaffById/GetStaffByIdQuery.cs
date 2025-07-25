using MediatR;
using Booklify.Application.Common.DTOs.Staff;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Staff.Queries.GetStaffById;

public record GetStaffByIdQuery(Guid Id) : IRequest<Result<StaffResponse>>; 