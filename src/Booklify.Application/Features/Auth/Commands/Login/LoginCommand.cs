using Booklify.Application.Common.DTOs.Auth;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.Auth.Commands.Login;

public record LoginCommand(LoginRequest LoginRequest) : IRequest<Result<AuthenticationResponse>>;