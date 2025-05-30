using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Booklify.API.Middlewares;
using Booklify.Application.Common.Interfaces;
using Booklify.Infrastructure.Services;

namespace Booklify.API.Injection;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add HttpContextAccessor
        services.AddHttpContextAccessor();
        
        // Register role-based access filters
        services.AddScoped<UserRoleAccessFilter>();
        services.AddScoped<StaffRoleAccessFilter>();
        services.AddScoped<AdminRoleAccessFilter>();

        // Register validation configuration
        services.AddValidationConfiguration();


        return services;
    }

    public static IApplicationBuilder UseApiConfiguration(this IApplicationBuilder app)
    {
        // Use global exception handling
        app.UseGlobalExceptionHandling();

        // Use JWT middleware
        app.UseJwtMiddleware();

        return app;
    }
} 