using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Booklify.Application.Common.DTOs.Auth;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Entities.Identity;
using Booklify.Domain.Enums;
using Booklify.Infrastructure.Models;

namespace Booklify.Infrastructure.Services;

/// <summary>
/// Implementation of identity service for authentication and user management
/// </summary>
public class IdentityService : IIdentityService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IJwtService _jwtService;
    private readonly JwtSettings _jwtSettings;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public IdentityService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IJwtService jwtService,
        IOptions<JwtSettings> jwtSettings,
        ICurrentUserService currentUserService,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _jwtSettings = jwtSettings.Value;
        _currentUserService = currentUserService;
        _httpContextAccessor = httpContextAccessor;
    }
    
    /// <summary>
    /// Authenticate a user and return AppUser if successful
    /// </summary>
    public async Task<Result<AppUser>> AuthenticateAsync(string username, string password, string? grantType = null)
    {
        // Find user by username
        var user = await _userManager.FindByNameAsync(username);
        
        if (user == null)
        {
            return Result<AppUser>.Failure("Username or password is incorrect", ErrorCode.InvalidCredentials);
        }
        
        // Check if email is confirmed
        if (!user.EmailConfirmed)
        {
            return Result<AppUser>.Failure("Please verify your email first", ErrorCode.EmailNotConfirmed);
        }
        
        // Verify password
        var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
        
        if (!result.Succeeded)
        {
            return Result<AppUser>.Failure("Username or password is incorrect", ErrorCode.InvalidCredentials);
        }
        
        return Result<AppUser>.Success(user);
    }
    
    /// <summary>
    /// Re-authenticate a user using refresh token
    /// </summary>
    public async Task<Result<AppUser>> ReAuthenticateAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        
        if (user == null)
        {
            return Result<AppUser>.Failure("User not found", ErrorCode.UserNotFound);
        }

        // Validate refresh token
        if (string.IsNullOrEmpty(user.RefreshToken))
        {
            return Result<AppUser>.Failure("Refresh token not found", ErrorCode.Unauthorized);
        }

        if (!user.RefreshTokenExpiryTime.HasValue || user.RefreshTokenExpiryTime.Value <= DateTime.UtcNow)
        {
            // Clear expired refresh token
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _userManager.UpdateAsync(user);
            
            return Result<AppUser>.Failure("Refresh token expired", ErrorCode.Unauthorized);
        }

        return Result<AppUser>.Success(user);
    }
    
    /// <summary>
    /// Create a refresh token for a user
    /// </summary>
    public async Task<string> GenerateRefreshTokenAsync(AppUser user)
    {
        // Generate random token
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var refreshToken = Convert.ToBase64String(randomNumber);
        
        // Store token in user
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiresInDays);
        
        await _userManager.UpdateAsync(user);
        
        return refreshToken;
    }
    
    /// <summary>
    /// Register a new user account
    /// </summary>
    public async Task<Result<AppUser>> RegisterAsync(AppUser user, string password)
    {
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return Result<AppUser>.Failure("Create account failed", ErrorCode.ValidationFailed, errors);
        }
        return Result<AppUser>.Success(user);
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    public async Task<Result<UserRegistrationResponse>> RegisterUserAsync(UserRegistrationRequest request)
    {
        // Create identity user - INACTIVE until email confirmed
        var user = new AppUser
        {
            UserName = request.Username,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            EmailConfirmed = false,  // Must verify email first
            IsActive = false,        // Account inactive until email verified
        };
        
        var result = await _userManager.CreateAsync(user, request.Password);
        
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return Result<UserRegistrationResponse>.Failure("Create account failed", ErrorCode.ValidationFailed, errors);
        }
        
        // Assign role
        var roleResult = await _userManager.AddToRoleAsync(user, Role.User.ToString());
        if (!roleResult.Succeeded)
        {
            var errors = roleResult.Errors.Select(e => e.Description).ToList();
            return Result<UserRegistrationResponse>.Failure("Assign role failed", ErrorCode.ValidationFailed, errors);
        }
            
        // Return response
        return Result<UserRegistrationResponse>.Success(new UserRegistrationResponse
        {
            UserId = user.Id,
            Username = user.UserName,
            Email = user.Email ?? string.Empty
        });
    }
    
    /// <summary>
    /// Register a new staff account
    /// </summary>
    public async Task<Result<StaffRegistrationResponse>> RegisterStaffAsync(StaffRegistrationRequest request)
    {
        // Create identity user
        var user = new AppUser
        {
            UserName = request.Username,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
        };
        
        var result = await _userManager.CreateAsync(user, request.Password);
        
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return Result<StaffRegistrationResponse>.Failure("Create account failed", ErrorCode.ValidationFailed, errors);
        }
        
        // Assign role
        var roleResult = await _userManager.AddToRoleAsync(user, Role.Staff.ToString());
        if (!roleResult.Succeeded)
        {
            var errors = roleResult.Errors.Select(e => e.Description).ToList();
            return Result<StaffRegistrationResponse>.Failure("Assign role failed", ErrorCode.ValidationFailed, errors);
        }
            
        // Return response - StaffId will be set by CQRS handler after creating entity
        return Result<StaffRegistrationResponse>.Success(new StaffRegistrationResponse
        {
            UserId = user.Id,
            Username = user.UserName,
            Email = user.Email ?? string.Empty
        });
    }
    
    /// <summary>
    /// Update EntityId for a user
    /// </summary>
    public async Task<Result> UpdateEntityIdAsync(string userId, Guid entityId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        
        if (user == null)
        {
            return Result.Failure("User not found", ErrorCode.UserNotFound);
        }
        
        user.EntityId = entityId;
        
        var result = await _userManager.UpdateAsync(user);
        
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return Result.Failure("Update information failed", ErrorCode.ValidationFailed, errors);
        }
        
        return Result.Success();
    }
    
    /// <summary>
    /// Change user password
    /// </summary>
    public async Task<Result> ChangePasswordAsync(string userId, string oldPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        
        if (user == null)
        {
            return Result.Failure("User not found", ErrorCode.UserNotFound);
        }
        
        if (user.IsActive == false)
        {
            return Result.Failure("Account is not active", ErrorCode.Unauthorized);
        }

        var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
        
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            // Check if error is about incorrect password
            if (errors.Any(e => e.Contains("Incorrect password")))
            {
                return Result.Failure("Old password is incorrect", ErrorCode.InvalidCredentials, errors);
            }
            return Result.Failure("Change password failed", ErrorCode.ValidationFailed, errors);
        }
        
        return Result.Success("Change password success");
    }
    
    /// <summary>
    /// Change current user's password (uses ICurrentUserService internally)
    /// </summary>
    public async Task<Result> ChangePasswordAsync(string oldPassword, string newPassword)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure("User is not authenticated", ErrorCode.Unauthorized);
        }
        
        return await ChangePasswordAsync(userId, oldPassword, newPassword);
    }
    
    /// <summary>
    /// Update FCM token for user
    /// </summary>
    public async Task<Result<List<string>>> UpdateFcmTokenAsync(string userId, string fcmToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        
        if (user == null)
        {
            return Result<List<string>>.Failure("User not found", ErrorCode.UserNotFound);
        }
        
        // Store FCM token logic here
        // This is a simplified implementation
        var tokens = new List<string> { fcmToken };
        
        return Result<List<string>>.Success(tokens);
    }
    
    /// <summary>
    /// Update FCM token for current user
    /// </summary>
    public async Task<Result<List<string>>> UpdateFcmTokenAsync(string fcmToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result<List<string>>.Failure("User is not authenticated", ErrorCode.Unauthorized);
        }
        
        return await UpdateFcmTokenAsync(userId, fcmToken);
    }
    
    /// <summary>
    /// Logout a user
    /// </summary>
    public async Task<Result> LogoutAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        
        if (user == null)
        {
            return Result.Failure("User not found", ErrorCode.UserNotFound);
        }

        if (user.IsActive == false)
        {
            return Result.Failure("Account is not active", ErrorCode.Unauthorized);
        }
        
        // Clear refresh token
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        
        await _userManager.UpdateAsync(user);
        
        return Result.Success("Logout success");
    }
    
    /// <summary>
    /// Logout current user
    /// </summary>
    public async Task<Result> LogoutAsync()
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure("User is not authenticated", ErrorCode.Unauthorized);
        }
        
        return await LogoutAsync(userId);
    }

    public async Task<Result<bool>> IsPhoneUniqueAsync(string phone)
    {
        var exist = await _userManager.Users.AnyAsync(x => x.PhoneNumber == phone);
        return Result<bool>.Success(!exist);
    }

    public async Task<Result<bool>> IsEmailUniqueAsync(string email)
    {
        var exist = await _userManager.Users.AnyAsync(x => x.Email == email);
        return Result<bool>.Success(!exist);
    }

    /// <summary>
    /// Get roles for a user
    /// </summary>
    public async Task<IList<string>> GetUserRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new List<string>();
        }
        
        return await _userManager.GetRolesAsync(user);
    }

    public async Task<Result<AppUser?>> FindByUsernameAsync(string username)
    {
        var user = await _userManager.FindByNameAsync(username);
        return Result<AppUser?>.Success(user);
    }

    public async Task<IdentityResult> AddToRoleAsync(AppUser user, string role)
    {
        return await _userManager.AddToRoleAsync(user, role);
    }

    public async Task<Result<AppUser?>> FindByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return Result<AppUser?>.Success(user);
    }

    public async Task<Result<AppUser?>> FindByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return Result<AppUser?>.Success(user);
    }

    public async Task<Result> UpdateUserNameAsync(string userId, string newUsername)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result.Failure("User not found", ErrorCode.UserNotFound);
            }
            
            // Check if username is already the same
            if (string.Equals(user.UserName, newUsername, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Success("Username is already the same, no need to update");
            }
            
            // Check if new username conflicts with existing user
            var existingUser = await _userManager.FindByNameAsync(newUsername);
            if (existingUser != null && existingUser.Id != userId)
            {
                return Result.Failure($"Username {newUsername} is already used by another user", ErrorCode.DuplicateEntry);
            }
            
            user.UserName = newUsername;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return Result.Failure("Update username failed", ErrorCode.ValidationFailed, errors);
            }
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error updating username: {ex.Message}", ErrorCode.InternalError);
        }
    }

    public async Task<Result<bool>> IsUsernameUniqueAsync(string username)
    {
        var exist = await _userManager.Users.AnyAsync(x => x.UserName == username);
        return Result<bool>.Success(!exist);
    }

    public async Task<Result> UpdateUserAsync(AppUser user)
    {
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return Result.Failure("Update user failed", ErrorCode.ValidationFailed, errors);
        }
        return Result.Success();
    }

    public async Task<bool> IsUserIdExist(string userId)
    {
        var exist = await _userManager.Users.AnyAsync(x => x.Id == userId);
        return exist;
    }

    public async Task<bool> DeleteUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }
        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded;
    }

    public async Task<Result> ResetPasswordAsync(string userId, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Failure("User not found", ErrorCode.UserNotFound);
        }

        // Remove existing password
        await _userManager.RemovePasswordAsync(user);

        // Add new password
        var result = await _userManager.AddPasswordAsync(user, newPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return Result.Failure("Reset password failed", ErrorCode.ValidationFailed, errors);
        }

        return Result.Success("Reset password success");
    }

    public async Task<Result<IEnumerable<string>>> GetAllUsernamesAsync()
    {
        try
        {
            var usernames = await _userManager.Users
                .Where(u => !string.IsNullOrEmpty(u.UserName))
                .Select(u => u.UserName)
                .ToListAsync();
            
            return Result<IEnumerable<string>>.Success(usernames);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<string>>.Failure("Error when getting list username", ErrorCode.InternalError);
        }
    }

    /// <summary>
    /// Register multiple users in batch with role assignment
    /// </summary>
    public async Task<Result<List<AppUser>>> RegisterRangeAsync(List<AppUser> users, string password, string role)
    {
        var successfulUsers = new List<AppUser>();
        var errors = new List<string>();

        foreach (var user in users)
        {
            try
            {
                // Create user
                var createResult = await _userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    errors.AddRange(createResult.Errors.Select(e => $"User {user.UserName}: {e.Description}"));
                    continue;
                }

                // Assign role
                var roleResult = await _userManager.AddToRoleAsync(user, role);
                if (!roleResult.Succeeded)
                {
                    errors.AddRange(roleResult.Errors.Select(e => $"Role assignment for {user.UserName}: {e.Description}"));
                    // User created but role failed - still consider it successful for now
                }

                successfulUsers.Add(user);
            }
            catch (Exception ex)
            {
                errors.Add($"Exception for user {user.UserName}: {ex.Message}");
            }
        }

        if (errors.Any() && !successfulUsers.Any())
        {
            return Result<List<AppUser>>.Failure("All accounts creation failed", ErrorCode.ValidationFailed, errors);
        }

        if (errors.Any())
        {
            return Result<List<AppUser>>.Success(successfulUsers, 
                $"Create {successfulUsers.Count}/{users.Count} accounts success");
        }

        return Result<List<AppUser>>.Success(successfulUsers, 
            $"Create all {successfulUsers.Count} accounts success");
    }

    /// <summary>
    /// Register multiple users in strict mode - all must succeed or all fail
    /// </summary>
    public async Task<Result<List<AppUser>>> RegisterRangeStrictAsync(List<AppUser> users, string password, string role)
    {
        var createdUsers = new List<AppUser>();
        var errors = new List<string>();

        try
        {
            // Pre-validate for potential conflicts
            var userIds = users.Select(u => u.Id).ToList();
            var userNames = users.Select(u => u.UserName).ToList();
            
            // Check for duplicates within the batch
            if (userIds.Count != userIds.Distinct().Count())
            {
                var duplicateIds = userIds.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key);
                errors.Add($"Duplicate User IDs in batch: {string.Join(", ", duplicateIds)}");
                return Result<List<AppUser>>.Failure("Duplicate User ID in batch", ErrorCode.DuplicateEntry, errors);
            }
            
            if (userNames.Count != userNames.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            {
                var duplicateUsernames = userNames.GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1).Select(g => g.Key);
                errors.Add($"Duplicate usernames trong batch: {string.Join(", ", duplicateUsernames)}");
                return Result<List<AppUser>>.Failure("Duplicate username in batch", ErrorCode.DuplicateEntry, errors);
            }

            // Check for existing conflicts in database
            var existingIds = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync();
                
            if (existingIds.Any())
            {
                errors.Add($"User IDs already exist in database: {string.Join(", ", existingIds)}");
                return Result<List<AppUser>>.Failure("User ID already exists", ErrorCode.DuplicateEntry, errors);
            }
            
            var existingUsernames = await _userManager.Users
                .Where(u => userNames.Contains(u.UserName))
                .Select(u => u.UserName)
                .ToListAsync();
                
            if (existingUsernames.Any())
            {
                errors.Add($"Usernames already exist in database: {string.Join(", ", existingUsernames)}");
                return Result<List<AppUser>>.Failure("Username already exists", ErrorCode.DuplicateEntry, errors);
            }

            // Create users one by one with detailed error tracking
            foreach (var user in users)
            {
                try
                {
                    // Create user
                    var createResult = await _userManager.CreateAsync(user, password);
                    if (!createResult.Succeeded)
                    {
                        var userErrors = createResult.Errors.Select(e => $"User {user.UserName} (ID: {user.Id}): {e.Description}");
                        errors.AddRange(userErrors);
                        throw new InvalidOperationException($"Failed to create user {user.UserName}: {string.Join("; ", userErrors)}");
                    }

                    // Assign role
                    var roleResult = await _userManager.AddToRoleAsync(user, role);
                    if (!roleResult.Succeeded)
                    {
                        var roleErrors = roleResult.Errors.Select(e => $"Role assignment for {user.UserName}: {e.Description}");
                        errors.AddRange(roleErrors);
                        throw new InvalidOperationException($"Failed to assign role for user {user.UserName}: {string.Join("; ", roleErrors)}");
                    }

                    createdUsers.Add(user);
                }
                catch (Exception userEx)
                {
                    errors.Add($"Exception for user {user.UserName} (ID: {user.Id}): {userEx.Message}");
                    throw; // Re-throw to trigger cleanup
                }
            }

            return Result<List<AppUser>>.Success(createdUsers, 
                $"Create all {createdUsers.Count} accounts success");
        }
        catch (Exception ex)
        {
            // If any user fails, we need to cleanup already created users
            foreach (var createdUser in createdUsers)
            {
                try
                {
                    await _userManager.DeleteAsync(createdUser);
                }
                catch (Exception cleanupEx)
                {
                    errors.Add($"Cleanup failed for {createdUser.UserName}: {cleanupEx.Message}");
                }
            }

            var finalError = $"Create multiple accounts failed: {ex.Message}";
            return Result<List<AppUser>>.Failure(finalError, ErrorCode.ValidationFailed, errors);
        }
    }
} 