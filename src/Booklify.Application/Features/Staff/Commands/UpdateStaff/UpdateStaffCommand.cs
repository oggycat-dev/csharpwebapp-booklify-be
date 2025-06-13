using Booklify.Application.Common.DTOs.Staff;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.Staff.Commands.UpdateStaff;

public record UpdateStaffCommand(Guid StaffId, UpdateStaffRequest Request) : IRequest<Result<StaffResponse>>; 