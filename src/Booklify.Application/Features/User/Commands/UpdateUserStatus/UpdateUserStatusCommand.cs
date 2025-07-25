using Booklify.Application.Common.DTOs.User;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.User.Commands.UpdateUserStatus;
 
public record UpdateUserStatusCommand(Guid UserId, UpdateUserStatusRequest Request) : IRequest<Result>; 