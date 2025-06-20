using MediatR;
using AutoMapper;
using Booklify.Application.Common.DTOs.Auth;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Entities;
using Booklify.Domain.Entities.Identity;
using Booklify.Domain.Enums;

namespace Booklify.Application.Features.Auth.Commands.Register;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<UserRegistrationResponse>>
{
    private readonly IIdentityService _identityService;
    private readonly IBooklifyDbContext _context;
    private readonly IMapper _mapper;

    public RegisterUserCommandHandler(
        IIdentityService identityService, 
        IBooklifyDbContext context,
        IMapper mapper)
    {
        _identityService = identityService;
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<UserRegistrationResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // Create identity user using IdentityService
        var identityResult = await _identityService.RegisterUserAsync(request.Request);
        
        if (!identityResult.IsSuccess)
        {
            return identityResult;
        }
        
        // Find the created user to get ID for UserProfile
        var userResult = await _identityService.FindByUsernameAsync(request.Request.Username);
        if (!userResult.IsSuccess || userResult.Data == null)
        {
            return Result<UserRegistrationResponse>.Failure("User not found after creation", ErrorCode.UserNotFound);
        }
        
        var user = userResult.Data;
        
        // Create UserProfile
        var userProfile = new UserProfile
        {
            IdentityUserId = user.Id,
            FirstName = string.Empty,
            LastName = string.Empty,
            FullName = request.Request.Username,
            Phone = request.Request.PhoneNumber,
            Status = EntityStatus.Active
        };
        
        _context.UserProfiles.Add(userProfile);
        await _context.SaveChangesAsync(cancellationToken);
        
        // Update EntityId in Identity User
        await _identityService.UpdateEntityIdAsync(user.Id, userProfile.Id);
        
        return identityResult;
    }
} 