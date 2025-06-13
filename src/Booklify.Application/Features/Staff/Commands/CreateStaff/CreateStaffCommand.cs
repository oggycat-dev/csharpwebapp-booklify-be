using Booklify.Application.Common.DTOs.Staff;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.Staff.Commands.CreateStaff;

public record CreateStaffCommand(CreateStaffRequest Request) : IRequest<Result<CreatedStaffResponse>>;