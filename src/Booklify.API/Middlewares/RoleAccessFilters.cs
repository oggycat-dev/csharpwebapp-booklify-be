using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Booklify.Application.Common.DTOs.Auth;
using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;

namespace Booklify.API.Middlewares;

/// <summary>
/// Base filter for system access control
/// </summary>
public abstract class SystemAccessFilterBase : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Request.Method == "OPTIONS")
            return;
            
        if (context.HttpContext.Request.Method == "POST" && 
            context.HttpContext.Request.HasJsonContentType())
        {
            context.HttpContext.Items["ProcessLoginResult"] = true;
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.HttpContext.Items["ProcessLoginResult"] == null)
            return;
            
        if (context.Result is not ObjectResult objectResult || objectResult.StatusCode != 200)
            return;
        
        if (objectResult.Value is not Result<AuthenticationResponse> authResult || !authResult.IsSuccess)
            return;
            
        var authResponse = authResult.Data;
        
        if (authResponse != null && !IsAuthorizedForSystem(authResponse))
        {
            var systemName = GetSystemName();
            var allowedRoles = GetAllowedRolesDescription();
            
            context.Result = new ObjectResult(Result.Failure(
                $"You do not have access to the {systemName}.",
                ErrorCode.InsufficientPermissions,
                $"This area is only accessible to {allowedRoles}. Please use an appropriate account."))
            {
                StatusCode = 403
            };
        }
    }
    
    protected abstract bool IsAuthorizedForSystem(AuthenticationResponse user);
    protected abstract string GetSystemName();
    protected abstract string GetAllowedRolesDescription();
    
    /// <summary>
    /// Check if the user has a specific role based on the AppRole list
    /// </summary>
    protected bool HasRole(AuthenticationResponse user, string role)
    {
        return user.AppRole?.Contains(role, StringComparer.OrdinalIgnoreCase) ?? false;
    }
}

/// <summary>
/// Filter that allows only user access
/// </summary>
public class UserRoleAccessFilter : SystemAccessFilterBase
{
    protected override bool IsAuthorizedForSystem(AuthenticationResponse user)
    {
        return HasRole(user, "User") || HasRole(user, "Staff") || HasRole(user, "Admin");
    }
    
    protected override string GetSystemName()
    {
        return "User Portal";
    }
    
    protected override string GetAllowedRolesDescription()
    {
        return "Users, Staff members, and Administrators";
    }
}

/// <summary>
/// Filter that allows only staff and admin access
/// </summary>
public class StaffRoleAccessFilter : SystemAccessFilterBase
{
    protected override bool IsAuthorizedForSystem(AuthenticationResponse user)
    {
        return HasRole(user, "Staff") || HasRole(user, "Admin");
    }
    
    protected override string GetSystemName()
    {
        return "Staff Portal";
    }
    
    protected override string GetAllowedRolesDescription()
    {
        return "Staff members and Administrators";
    }
}

/// <summary>
/// Filter that allows only admin and staff access to admin portal
/// </summary>
public class AdminRoleAccessFilter : SystemAccessFilterBase
{
    protected override bool IsAuthorizedForSystem(AuthenticationResponse user)
    {
        return HasRole(user, "Admin") || HasRole(user, "Staff");
    }
    
    protected override string GetSystemName()
    {
        return "Admin Portal";
    }
    
    protected override string GetAllowedRolesDescription()
    {
        return "Staff members and Administrators";
    }
} 