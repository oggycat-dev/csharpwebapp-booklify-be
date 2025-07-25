using MediatR;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Auth.Commands.ConfirmEmail;

/// <summary>
/// Command for confirming user email
/// </summary>
public record ConfirmEmailCommand(string Email, string Token) : IRequest<Result>; 