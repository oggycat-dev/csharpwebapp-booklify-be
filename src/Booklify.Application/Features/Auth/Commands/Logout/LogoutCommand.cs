using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.Auth.Commands.Logout;

public record LogoutCommand() : IRequest<Result>; 