using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Booklify.API.Configurations;

/// <summary>
/// Configuration for Swagger UI
/// </summary>
public static class SwaggerConfiguration
{
    /// <summary>
    /// Configure Swagger generation options
    /// </summary>
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            // Configure basic information
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Booklify API",
                Version = "v1",
                Description = "API for Booklify book management system"
            });
            
            // Configure API grouping by controller
            options.TagActionsBy(api =>
            {
                // Prioritize Tags from attributes on controller or action
                var controllerTags = api.ActionDescriptor.EndpointMetadata
                    .OfType<TagsAttribute>()
                    .SelectMany(attr => attr.Tags)
                    .Distinct();
                    
                if (controllerTags.Any())
                {
                    return controllerTags.ToList();
                }
                
                // Get controller name
                var controllerName = api.ActionDescriptor.RouteValues["controller"];
                
                // Determine main group based on path
                var relativePath = api.RelativePath?.ToLower();
                string mainTag;
                
                if (relativePath?.Contains("/admin/") == true)
                {
                    mainTag = "Admin";
                }
                else if (relativePath?.Contains("/user/") == true)
                {
                    mainTag = "User";
                }
                else if (relativePath?.Contains("/common/") == true)
                {
                    mainTag = "Common";
                }
                else
                {
                    return new[] { controllerName };
                }
                
                // Combine main group with controller name
                var combinedTag = $"{mainTag}_{controllerName}";
                return new[] { mainTag, combinedTag };
            });
            
            // Sort by tag
            options.OrderActionsBy(apiDesc => $"{apiDesc.GroupName}");
            
            // Configure JWT authentication in Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
            
            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
            
            // Customize operation IDs
            options.CustomOperationIds(apiDesc =>
            {
                if (apiDesc.ActionDescriptor is ControllerActionDescriptor descriptor)
                {
                    var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"];
                    return $"{controllerName}_{descriptor.MethodInfo.Name}";
                }
                return null;
            });
        });
        
        return services;
    }
    
    /// <summary>
    /// Configure Swagger middleware
    /// </summary>
    public static IApplicationBuilder UseSwaggerConfiguration(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseSwagger();
        
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Booklify API v1");
            options.RoutePrefix = "swagger";
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            options.EnableDeepLinking();
            options.DisplayOperationId();
            options.EnableFilter();
            options.EnableTryItOutByDefault();
            
            // Add custom CSS
            options.InjectStylesheet("/swagger-custom/custom-swagger-ui.css");
            
            // Custom head content to set favicon and title
            options.HeadContent = @"
                <link rel=""icon"" type=""image/png"" href=""/swagger-custom/favicon.png"" sizes=""32x32"" />
                <style>
                    .swagger-ui .topbar { display: flex; }
                    .swagger-ui .topbar-wrapper { width: 100%; }
                </style>
            ";
        });
        
        return app;
    }
}

/// <summary>
/// Attribute to specify application group and controller group for API controllers
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class TagsAttribute : Attribute
{
    /// <summary>
    /// Gets the tags used by the action or controller
    /// </summary>
    public string[] Tags { get; }

    /// <summary>
    /// Creates a new TagsAttribute with the specified tags
    /// </summary>
    /// <param name="tags">The tags to apply to the action or controller</param>
    public TagsAttribute(params string[] tags)
    {
        Tags = tags;
    }
} 