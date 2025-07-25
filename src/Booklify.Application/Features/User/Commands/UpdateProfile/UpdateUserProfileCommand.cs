using Booklify.Application.Common.DTOs.User;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.User.Commands.UpdateProfile;

public record UpdateUserProfileCommand(UpdateUserProfileRequest Request) : IRequest<Result>; 