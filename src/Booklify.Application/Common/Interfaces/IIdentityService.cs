using Booklify.Application.Common.DTOs.Auth;
using Booklify.Application.Common.Models;
using Booklify.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace Booklify.Application.Common.Interfaces;

/// <summary>
/// Service interface for handling identity and authentication operations
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Authenticate a user and return AppUser if successful
    /// </summary>
    Task<Result<AppUser>> AuthenticateAsync(string username, string password, string? grantType = null);
    
    /// <summary>
    /// Re-authenticate a user using refresh token
    /// </summary>
    Task<Result<AppUser>> ReAuthenticateAsync(string userId);
    
    /// <summary>
    /// Creates a refresh token for a user
    /// </summary>
    Task<string> GenerateRefreshTokenAsync(AppUser user);
    
    /// <summary>
    /// Register a new user account
    /// </summary>
    Task<Result<AppUser>> RegisterAsync(AppUser user, string password);

    /// <summary>
    /// Register a new user account
    /// </summary>
    Task<Result<UserRegistrationResponse>> RegisterUserAsync(UserRegistrationRequest request);
    
    /// <summary>
    /// Register a new staff account
    /// </summary>
    Task<Result<StaffRegistrationResponse>> RegisterStaffAsync(StaffRegistrationRequest request);
    
    /// <summary>
    /// Change user password
    /// </summary>
    Task<Result> ChangePasswordAsync(string userId, string oldPassword, string newPassword);
    
    /// <summary>
    /// Change current user's password (uses ICurrentUserService internally)
    /// </summary>
    Task<Result> ChangePasswordAsync(string oldPassword, string newPassword);
    
    /// <summary>
    /// Update FCM (Firebase Cloud Messaging) token for notifications
    /// </summary>
    Task<Result<List<string>>> UpdateFcmTokenAsync(string userId, string fcmToken);
    
    /// <summary>
    /// Update FCM token for current user (uses ICurrentUserService internally)
    /// </summary>
    Task<Result<List<string>>> UpdateFcmTokenAsync(string fcmToken);
    
    /// <summary>
    /// Logout a user and invalidate their refresh token
    /// </summary>
    Task<Result> LogoutAsync(string userId);
    
    /// <summary>
    /// Logout current user (uses ICurrentUserService internally)
    /// </summary>
    Task<Result> LogoutAsync();
    
    /// <summary>
    /// Update EntityId for a user
    /// </summary>
    Task<Result> UpdateEntityIdAsync(string userId, Guid entityId);

    /// <summary>
    /// Check if a phone number is unique
    /// </summary>
    Task<Result<bool>> IsPhoneUniqueAsync(string phone);

    /// <summary>
    /// Check if an email is unique
    /// </summary>
    Task<Result<bool>> IsEmailUniqueAsync(string email);

    /// <summary>
    /// Check if a username is unique
    /// </summary>
    Task<Result<bool>> IsUsernameUniqueAsync(string username);

    /// <summary>
    /// Get roles for a user
    /// </summary>
    Task<IList<string>> GetUserRolesAsync(string userId);

    /// <summary>
    /// Find a user by username
    /// </summary>
    Task<Result<AppUser?>> FindByUsernameAsync(string username);

    /// <summary>
    /// Add a user to a role
    /// </summary>
    Task<IdentityResult> AddToRoleAsync(AppUser user, string role);
    
    /// <summary>
    /// Find a user by ID
    /// </summary>
    Task<Result<AppUser?>> FindByIdAsync(string userId);
    
    /// <summary>
    /// Find a user by email
    /// </summary>
    Task<Result<AppUser?>> FindByEmailAsync(string email);
    
    /// <summary>
    /// Update a user's username
    /// </summary>
    Task<Result> UpdateUserNameAsync(string userId, string newUsername);

    /// <summary>
    /// Update a user's information in the Identity system
    /// </summary>
    Task<Result> UpdateUserAsync(AppUser user);

    /// <summary>
    /// Check if a user ID exists
    /// </summary>
    Task<bool> IsUserIdExist(string userId);

    /// <summary>
    /// Delete a user
    /// </summary>
    Task<bool> DeleteUser(string userId);

    /// <summary>
    /// Reset a user's password
    /// </summary>
    Task<Result> ResetPasswordAsync(string userId, string newPassword);

    /// <summary>
    /// Get all usernames (emails) in the system for duplicate checking
    /// </summary>
    Task<Result<IEnumerable<string>>> GetAllUsernamesAsync();

    /// <summary>
    /// Register multiple users in batch with role assignment
    /// </summary>
    Task<Result<List<AppUser>>> RegisterRangeAsync(List<AppUser> users, string password, string role);

    /// <summary>
    /// Register multiple users in strict mode - all must succeed or all fail
    /// </summary>
    Task<Result<List<AppUser>>> RegisterRangeStrictAsync(List<AppUser> users, string password, string role);
}