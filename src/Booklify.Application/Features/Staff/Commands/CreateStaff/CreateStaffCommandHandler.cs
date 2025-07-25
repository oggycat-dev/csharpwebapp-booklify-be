using AutoMapper;
using Booklify.Application.Common.DTOs.Staff;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Commons;
using Booklify.Domain.Entities;
using Booklify.Domain.Entities.Identity;
using Booklify.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Transactions;

namespace Booklify.Application.Features.Staff.Commands.CreateStaff;

public class CreateStaffCommandHandler : IRequestHandler<CreateStaffCommand, Result<CreatedStaffResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateStaffCommandHandler> _logger;
    private readonly IIdentityService _identityService;
    
    public CreateStaffCommandHandler(
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ICurrentUserService currentUserService, 
        ILogger<CreateStaffCommandHandler> logger,
        IIdentityService identityService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
        _identityService = identityService;
    }
    
    public async Task<Result<CreatedStaffResponse>> Handle(CreateStaffCommand command, CancellationToken cancellationToken)
    {
        var isUserValid = await _currentUserService.IsUserValidAsync();
        if (!isUserValid)
        {
            return Result<CreatedStaffResponse>.Failure(
                "User is not authenticated", 
                ErrorCode.Unauthorized);
        }

        var currentUserId = _currentUserService.UserId;
        StaffProfile staffProfile;
        
        try
        {
            using (var scope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted,
                    Timeout = TimeSpan.FromMinutes(1)
                },
                TransactionScopeAsyncFlowOption.Enabled
            ))
            {
                // 1. Create user for identity first
                var user = new AppUser
                {
                    UserName = command.Request.Email,
                    Email = command.Request.Email,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true
                };

                var createdUserResult = await _identityService.RegisterAsync(user, command.Request.Password);
                if (!createdUserResult.IsSuccess)
                {
                    return Result<CreatedStaffResponse>.Failure(
                        createdUserResult.Message, 
                        ErrorCode.InternalError);
                }
                
                // 2. Assign Staff role
                var roleResult = await _identityService.AddToRoleAsync(createdUserResult.Data!, Role.Staff.ToString());
                if (!roleResult.Succeeded)
                {
                    return Result<CreatedStaffResponse>.Failure(
                        "Failed to add staff to role", 
                        ErrorCode.InternalError);
                }

                // 3. Create staff profile with correct IdentityUserId
                staffProfile = _mapper.Map<StaffProfile>(command.Request);
                BaseEntityExtensions.InitializeBaseEntity(staffProfile, currentUserId);
                staffProfile.IdentityUserId = createdUserResult.Data!.Id;

                await _unitOfWork.StaffProfileRepository.AddAsync(staffProfile);
                
                // 4. Update user EntityId to link back to staff profile
                var updateEntityResult = await _identityService.UpdateEntityIdAsync(createdUserResult.Data.Id, staffProfile.Id);
                if (!updateEntityResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to update user EntityId for staff profile linking: {Error}", updateEntityResult.Message);
                    // This is not critical enough to fail the entire operation
                }
                
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                scope.Complete();
            }
            
            // 7. Map to response
            var response = _mapper.Map<CreatedStaffResponse>(staffProfile);
            return Result<CreatedStaffResponse>.Success(response, "Staff created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating staff");
            return Result<CreatedStaffResponse>.Failure(
                "Error creating staff", 
                ErrorCode.InternalError);
        }
    }
}