using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Booklify.Infrastructure.Persistence;

public class BooklifyDbContextFactory : IDesignTimeDbContextFactory<BooklifyDbContext>
{
    public BooklifyDbContext CreateDbContext(string[] args)
    {
        // Load environment variables from .env file
        DotNetEnv.Env.Load(Path.Combine(Directory.GetCurrentDirectory(), "../Booklify.API/.env"));
        
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        
        // Build configuration
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string not found. Please set the CONNECTION_STRING environment variable or configure it in appsettings.json");
        }

        var optionsBuilder = new DbContextOptionsBuilder<BooklifyDbContext>();
        optionsBuilder.UseSqlServer(connectionString, 
            sqlOptions => 
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                    
                sqlOptions.MigrationsAssembly(typeof(BooklifyDbContext).Assembly.FullName);
            });

        return new BooklifyDbContext(optionsBuilder.Options);
    }
} 