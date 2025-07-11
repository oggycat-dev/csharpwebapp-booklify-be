using Booklify.Application.Common.DTOs.User;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.User.Queries.GetUsers;

public record GetUsersQuery(UserFilterModel? Filter = null) : IRequest<PaginatedResult<UserResponse>>;