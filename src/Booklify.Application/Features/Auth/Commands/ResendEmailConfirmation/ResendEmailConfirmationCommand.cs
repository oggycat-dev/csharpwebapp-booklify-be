using MediatR;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Auth.Commands.ResendEmailConfirmation;

/// <summary>
/// Command for resending email confirmation
/// </summary>
public record ResendEmailConfirmationCommand(string Email) : IRequest<Result>; 