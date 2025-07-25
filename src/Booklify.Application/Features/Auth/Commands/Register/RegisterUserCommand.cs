using MediatR;
using Booklify.Application.Common.DTOs.Auth;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Auth.Commands.Register;

public record RegisterUserCommand(UserRegistrationRequest Request) : IRequest<Result<UserRegistrationResponse>>; 