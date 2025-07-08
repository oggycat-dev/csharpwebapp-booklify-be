using AutoMapper;
using Booklify.Application.Common.DTOs.Auth;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthenticationResponse>>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtService _jwtService;
    private readonly IMapper _mapper;

    public LoginCommandHandler(IIdentityService identityService, IJwtService jwtService, IMapper mapper)
    {
        _identityService = identityService;
        _jwtService = jwtService;
        _mapper = mapper;
    }

    public async Task<Result<AuthenticationResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Authenticate user
        var authResult = await _identityService.AuthenticateAsync(
            request.LoginRequest.Username,
            request.LoginRequest.Password,
            request.LoginRequest.GrantType);

        if (!authResult.IsSuccess)
        {
            return Result<AuthenticationResponse>.Failure(
                authResult.Message,
                authResult.ErrorCode ?? ErrorCode.InvalidCredentials,
                authResult.Errors);
        }

        var user = authResult.Data!;
        
        // Check if user is active
        if (!user.IsActive)
        {
            return Result<AuthenticationResponse>.Failure(
                "Account is not active",
                ErrorCode.Unauthorized);
        }

        // Generate tokens and get expiration info
        var (accessToken, tokenRoles, expiresInMinutes, expiresAt) = _jwtService.GenerateJwtTokenWithExpiration(user);
        var refreshToken = await _identityService.GenerateRefreshTokenAsync(user);

        // Map user to response using AutoMapper
        var response = _mapper.Map<AuthenticationResponse>(user);
        
        // Set token-specific fields manually (as they're ignored in mapping)
        response.AppRole = tokenRoles;
        response.AccessToken = accessToken;
        response.TokenExpiresIn = expiresInMinutes;
        response.TokenExpiresAt = expiresAt;

        return Result<AuthenticationResponse>.Success(response, "Login success");
    }
}