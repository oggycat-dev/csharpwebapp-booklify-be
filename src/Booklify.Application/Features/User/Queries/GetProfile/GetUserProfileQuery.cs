using Booklify.Application.Common.DTOs.User;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.User.Queries.GetProfile;

public record GetUserProfileQuery() : IRequest<Result<UserDetailResponse>>; 