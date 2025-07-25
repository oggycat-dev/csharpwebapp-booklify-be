using AutoMapper;
using Booklify.Application.Common.DTOs.Auth;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.Auth.Queries.ReAuthenticate;

public class ReAuthenticateQueryHandler : IRequestHandler<ReAuthenticateQuery, Result<AuthenticationResponse>>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtService _jwtService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public ReAuthenticateQueryHandler(
        IIdentityService identityService, 
        IJwtService jwtService,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _identityService = identityService;
        _jwtService = jwtService;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<Result<AuthenticationResponse>> Handle(ReAuthenticateQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result<AuthenticationResponse>.Failure(
                "User is not authenticated", 
                ErrorCode.Unauthorized);
        }

        // Re-authenticate user
        var authResult = await _identityService.ReAuthenticateAsync(userId);
        if (!authResult.IsSuccess)
        {
            return Result<AuthenticationResponse>.Failure(
                authResult.Message,
                authResult.ErrorCode ?? ErrorCode.Unauthorized,
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

        // Generate new tokens and get expiration info
        var (accessToken, tokenRoles, expiresInMinutes, expiresAt) = _jwtService.GenerateJwtTokenWithExpiration(user);
        var refreshToken = await _identityService.GenerateRefreshTokenAsync(user);

        // Map user to response using AutoMapper
        var response = _mapper.Map<AuthenticationResponse>(user);
        
        // Set token-specific fields manually (as they're ignored in mapping)
        response.AppRole = tokenRoles;
        response.AccessToken = accessToken;
        response.TokenExpiresIn = expiresInMinutes;
        response.TokenExpiresAt = expiresAt;

        return Result<AuthenticationResponse>.Success(response, "Refresh token success");
    }
} 