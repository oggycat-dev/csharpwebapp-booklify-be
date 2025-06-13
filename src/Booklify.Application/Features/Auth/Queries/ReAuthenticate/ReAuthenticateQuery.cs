using Booklify.Application.Common.DTOs.Auth;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.Auth.Queries.ReAuthenticate;

public record ReAuthenticateQuery() : IRequest<Result<AuthenticationResponse>>; 