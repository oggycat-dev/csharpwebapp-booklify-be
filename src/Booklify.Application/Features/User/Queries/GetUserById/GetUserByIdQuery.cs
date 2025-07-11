using Booklify.Application.Common.DTOs.User;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.User.Queries.GetUserById;

public record GetUserByIdQuery(Guid Id) : IRequest<Result<UserDetailResponse>>; 