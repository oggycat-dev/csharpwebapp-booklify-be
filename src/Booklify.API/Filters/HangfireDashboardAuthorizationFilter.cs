using Hangfire.Dashboard;

namespace Booklify.API.Filters;

/// <summary>
/// Authorization filter for Hangfire dashboard
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    /// <summary>
    /// Authorize access to Hangfire dashboard
    /// </summary>
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // In development, allow access without authentication
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            return true;
        }
        
        // In production, require authentication and specific roles
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            // Check if user has required roles for Booklify
            return httpContext.User.IsInRole("Admin");
        }
        
        return false;
    }
} 