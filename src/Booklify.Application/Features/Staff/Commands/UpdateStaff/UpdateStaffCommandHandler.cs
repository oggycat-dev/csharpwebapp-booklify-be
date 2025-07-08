using AutoMapper;
using Booklify.Application.Common.DTOs.Staff;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Transactions;

namespace Booklify.Application.Features.Staff.Commands.UpdateStaff;

public class UpdateStaffCommandHandler : IRequestHandler<UpdateStaffCommand, Result<StaffResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateStaffCommandHandler> _logger;
    private readonly IIdentityService _identityService;

    public UpdateStaffCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<UpdateStaffCommandHandler> logger,
        IIdentityService identityService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
        _identityService = identityService;
    }

    public async Task<Result<StaffResponse>> Handle(UpdateStaffCommand command, CancellationToken cancellationToken)
    {
        var isUserValid = await _currentUserService.IsUserValidAsync();
        if (!isUserValid)
        {
            return Result<StaffResponse>.Failure(
                "User is not authenticated",
                ErrorCode.Unauthorized);
        }

        Domain.Entities.StaffProfile existingStaff;
        // 1. Find existing staff with IdentityUser
        existingStaff = await _unitOfWork.StaffProfileRepository
            .GetFirstOrDefaultAsync(
                x => x.Id == command.StaffId,
                x => x.IdentityUser);

        if (existingStaff == null)
        {
            return Result<StaffResponse>.Failure(
                "Staff not found",
                ErrorCode.NotFound);
        }
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


                // 2. Apply partial updates (only update fields that are provided)
                var request = command.Request;
                bool hasChanges = false;

                if (!string.IsNullOrWhiteSpace(request.FirstName) && request.FirstName != existingStaff.FirstName)
                {
                    existingStaff.FirstName = request.FirstName;
                    hasChanges = true;
                }

                if (!string.IsNullOrWhiteSpace(request.LastName) && request.LastName != existingStaff.LastName)
                {
                    existingStaff.LastName = request.LastName;
                    hasChanges = true;
                }

                // Update FullName if FirstName or LastName changed
                if (hasChanges)
                {
                    existingStaff.FullName = $"{existingStaff.FirstName} {existingStaff.LastName}";
                }

                if (!string.IsNullOrWhiteSpace(request.Phone) && request.Phone != existingStaff.Phone)
                {
                    // Check if phone already exists
                    var phoneExists = await _unitOfWork.StaffProfileRepository
                        .AnyAsync(x => x.Phone == request.Phone && x.Id != command.StaffId);
                    if (phoneExists)
                    {
                        return Result<StaffResponse>.Failure(
                            "Phone number already exists",
                            ErrorCode.ValidationFailed);
                    }

                    existingStaff.Phone = request.Phone;
                    hasChanges = true;
                }

                if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != existingStaff.Email)
                {
                    // Check if email already exists
                    var emailExists = await _unitOfWork.StaffProfileRepository
                        .AnyAsync(x => x.Email == request.Email && x.Id != command.StaffId);
                    if (emailExists)
                    {
                        return Result<StaffResponse>.Failure(
                            "Email already exists",
                            ErrorCode.ValidationFailed);
                    }

                    existingStaff.Email = request.Email;

                    // Update IdentityUser email if exists (use separate method to avoid tracking conflicts)
                    if (existingStaff.IdentityUser != null)
                    {
                        var identityUserId = existingStaff.IdentityUser.Id;
                        
                        // Use separate IdentityService methods to avoid tracking conflicts
                        var userResult = await _identityService.FindByIdAsync(identityUserId);
                        if (userResult.IsSuccess && userResult.Data != null)
                        {
                            userResult.Data.Email = request.Email;
                            userResult.Data.UserName = request.Email;
                            await _identityService.UpdateUserAsync(userResult.Data);
                        }
                    }

                    hasChanges = true;
                }

                if (!string.IsNullOrWhiteSpace(request.Address) && request.Address != existingStaff.Address)
                {
                    existingStaff.Address = request.Address;
                    hasChanges = true;
                }

                if (request.Position.HasValue && request.Position != existingStaff.Position)
                {
                    existingStaff.Position = request.Position.Value;
                    hasChanges = true;
                }

                // Handle IsActive status synchronization between entities
                if (request.IsActive.HasValue)
                {
                    var newIsActive = request.IsActive.Value;

                    // Update IdentityUser.IsActive (use separate method to avoid tracking conflicts)
                    if (existingStaff.IdentityUser != null &&
                        newIsActive != existingStaff.IdentityUser.IsActive)
                    {
                        var identityUserId = existingStaff.IdentityUser.Id;
                        
                        // Use separate IdentityService methods to avoid tracking conflicts
                        var userResult = await _identityService.FindByIdAsync(identityUserId);
                        if (userResult.IsSuccess && userResult.Data != null)
                        {
                            userResult.Data.IsActive = newIsActive;
                            await _identityService.UpdateUserAsync(userResult.Data);
                        }
                        hasChanges = true;
                    }

                    // Synchronize StaffProfile.Status with IdentityUser.IsActive
                    var newStatus = newIsActive ? EntityStatus.Active : EntityStatus.Inactive;
                    if (existingStaff.Status != newStatus)
                    {
                        existingStaff.Status = newStatus;
                        hasChanges = true;
                    }
                }

                if (!hasChanges)
                {
                    return Result<StaffResponse>.Success(
                        _mapper.Map<StaffResponse>(existingStaff));
                }

                // 3. Update modified date
                existingStaff.ModifiedAt = DateTime.UtcNow;
                var currentUserId = _currentUserService.UserId;
                if (!string.IsNullOrEmpty(currentUserId) && Guid.TryParse(currentUserId, out var userGuid))
                {
                    existingStaff.ModifiedBy = userGuid;
                }

                // 4. Save changes
                await _unitOfWork.StaffProfileRepository.UpdateAsync(existingStaff);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Complete transaction
                scope.Complete();
            }

            // 5. Map to response (outside transaction scope)
            var response = _mapper.Map<StaffResponse>(existingStaff);
            return Result<StaffResponse>.Success(response, "Staff updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating staff with ID: {StaffId}", command.StaffId);
            return Result<StaffResponse>.Failure(
                "Error updating staff",
                ErrorCode.InternalError);
        }
    }
}