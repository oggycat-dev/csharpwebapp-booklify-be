using Booklify.Application.Common.DTOs.Auth;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.Auth.Commands.ChangePassword;

public record ChangePasswordCommand(ChangePasswordRequest Request) : IRequest<Result>; 