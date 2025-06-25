using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Serilog;
using Microsoft.AspNetCore.Identity;
using Booklify.API.Configurations;
using Booklify.API.Extensions;
using Booklify.API.Injection;
using Booklify.API.Middlewares;
using Booklify.Domain.Entities.Identity;
using Booklify.Infrastructure.Persistence;
using Booklify.Application;
using Booklify.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Json;
using Hangfire;
using Booklify.API.Filters;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Add logging configuration
builder.AddLoggingConfiguration();

// Add environment configuration from .env
builder.AddEnvironmentConfiguration();

// Get environment information
var isProduction = builder.Environment.IsProduction();
var isDevelopment = builder.Environment.IsDevelopment();

// Get logger to log startup information
var startupLogger = LoggingConfiguration.CreateStartupLogger();

// Log environment information
startupLogger.LogInformation($"Current Environment: {builder.Environment.EnvironmentName}");
startupLogger.LogInformation($"IsProduction: {isProduction}");
startupLogger.LogInformation($"IsDevelopment: {isDevelopment}");
startupLogger.LogInformation($"ConnectionString configured: {!string.IsNullOrEmpty(builder.Configuration.GetConnectionString("DefaultConnection"))}");
startupLogger.LogInformation($"App:Environment: {builder.Configuration["App:Environment"]}");

// Configure JSON serialization for HTTP json options
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// Configure ApiBehaviorOptions to handle validation errors
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true; // Let our custom ValidationFilterAttribute handle it
});

// Add configurations
builder.Services.AddSwaggerConfiguration();
builder.Services.AddCorsConfiguration(builder.Configuration);
builder.Services.AddJwtConfiguration(builder.Configuration);
builder.Services.AddHangfireServices(builder.Configuration);

// Add application and infrastructure services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add API services
builder.Services.AddApiServices(builder.Configuration);

// Add validation using filter
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilterAttribute>();
});

builder.Services.AddProblemDetails();
builder.Services.AddAuthorization();

// Add HTTP context accessor for current user service
builder.Services.AddHttpContextAccessor();

// Configure server options for large file uploads
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 600 * 1024 * 1024; // 600MB
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10);
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
});

// Configure form options for multipart uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 600 * 1024 * 1024; // 600MB
    options.ValueLengthLimit = 600 * 1024 * 1024; // 600MB
    options.MultipartHeadersLengthLimit = 600 * 1024 * 1024; // 600MB
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (isDevelopment)
{
    try
    {
        startupLogger.LogInformation("Initializing and migrating database...");
        await app.ApplyMigrations(startupLogger);
        app.EnsureDatabaseCreated(startupLogger);
        await app.SeedDataAsync(startupLogger);
        startupLogger.LogInformation("Database initialization completed successfully.");
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "An error occurred during database initialization!");
        throw;
    }
}

// IMPORTANT: UseExceptionHandler must come before our custom middleware
app.UseExceptionHandler(builder => { }); // Empty handler, our middleware will do the work

// Add our custom global exception handling middleware
app.UseGlobalExceptionHandling();

// Apply Swagger configuration
app.UseSwaggerConfiguration(app.Environment);

// Add static files middleware
app.UseStaticFiles();

if (isProduction)
{
    app.UseHttpsRedirection();
}

// First handle CORS
app.UseCorsConfiguration();

// Add routing early in the pipeline
app.UseRouting();

// Then handle JWT authentication before authorization
app.UseJwtMiddleware();
app.UseAuthentication();
app.UseAuthorization();

// Configure Hangfire dashboard with authorization
app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
});

// Initialize Hangfire recurring jobs
app.Services.UseHangfireConfiguration();

// Finally map the controllers
app.MapControllers();

try
{
    startupLogger.LogInformation("Starting web host");
    await app.RunAsync();
}
catch (Exception ex)
{
    startupLogger.LogError(ex, "Host terminated unexpectedly");
    throw;
}
finally
{
    // Ensure all logs are written
    Log.CloseAndFlush();
}
