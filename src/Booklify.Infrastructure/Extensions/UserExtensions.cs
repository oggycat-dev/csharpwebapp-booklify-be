using Microsoft.EntityFrameworkCore;
using Booklify.Application.Common.Models;
using Booklify.Domain.Entities.Identity;
using Booklify.Infrastructure.Persistence;

namespace Booklify.Infrastructure.Extensions;

public static class UserExtensions
{
    /// <summary>
    /// Kiểm tra nhanh xem user có tồn tại và active hay không
    /// </summary>
    /// <param name="dbContext">Application DB Context</param>
    /// <param name="userId">User ID từ JWT claim</param>
    /// <returns>true nếu user tồn tại và active</returns>
    public static async Task<bool> IsUserValidAsync(
        this ApplicationDbContext dbContext,
        string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        return await dbContext.Users
            .AsNoTracking() // Tối ưu performance vì không cần tracking
            .AnyAsync(u => 
                u.Id == userId && 
                u.IsActive && 
                u.RefreshToken != null &&
                u.RefreshTokenExpiryTime > DateTime.UtcNow);
    }

    /// <summary>
    /// Kiểm tra chi tiết tình trạng user
    /// </summary>
    /// <param name="dbContext">Application DB Context</param>
    /// <param name="userId">User ID từ JWT claim</param>
    /// <returns>UserValidationResult chứa thông tin chi tiết</returns>
    public static async Task<UserValidationResult> ValidateUserStatusAsync(
        this ApplicationDbContext dbContext,
        string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return new UserValidationResult 
            { 
                IsValid = false,
                Error = "Không tìm thấy ID người dùng"
            };
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return new UserValidationResult 
            { 
                IsValid = false,
                Error = "Người dùng không tồn tại"
            };
        }

        if (!user.IsActive)
        {
            return new UserValidationResult 
            { 
                IsValid = false,
                Error = "Người dùng đang bị khóa",
                User = user
            };
        }

        // Kiểm tra thêm lockout
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            return new UserValidationResult 
            { 
                IsValid = false,
                Error = "Tài khoản đang bị tạm khóa",
                User = user
            };
        }

        return new UserValidationResult 
        { 
            IsValid = true,
            User = user
        };
    }
} 