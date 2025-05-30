# Booklify API

A .NET 8 Web API project for Ebook and Learning English, following Clean Architecture principles.

## Project Structure

The solution follows Clean Architecture and is organized into the following projects:

- **Booklify.Domain**: Contains enterprise business rules and entities
  - `Entities`: Domain entities
  - `Enums`: Enumeration types
  - `Extensions`: Domain-specific extension methods
  - `Helpers`: Helper classes for domain logic
  - `Commons`: Shared domain components

- **Booklify.Application**: Contains application business rules
  - `Common`: Shared application components
  - `Features`: Application features and use cases

- **Booklify.Infrastructure**: Contains external concerns and implementations
  - `Filters`: Custom filters
  - `Extensions`: Infrastructure-specific extension methods
  - `Migrations`: Database migrations
  - `Services`: Implementation of application services
  - `Repositories`: Data access implementations
  - `Models`: Data models
  - `Persistence`: Database context and configurations
  - `Configurations`: Infrastructure configurations
  - `Factories`: Factory implementations

- **Booklify.API**: The web API layer
  - `Controllers`: API endpoints
  - `Filters`: API-specific filters
  - `Middlewares`: Custom middleware components
  - `Configurations`: API configurations
  - `Helpers`: API helper classes
  - `Extensions`: API-specific extensions
  - `Injection`: Dependency injection modules
  - `docs`: API documentation
  - `wwwroot`: Static files
  - `Logs`: Application logs

## Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or later / Visual Studio Code
- SQL Server (LocalDB or higher)

## Getting Started

1. Clone the repository
   
2. Navigate to the API project directory
   ```
   cd src/Booklify.API
   ```
3. Create a `.env` file based on the `.env.example` template and update the connection string with your database information

4. Restore dependencies:
   ```
   dotnet restore
   ```

5. The application will automatically apply migrations on startup. If you need to apply migrations manually, use the following commands:

   For ApplicationDbContext:
   ```
   dotnet ef database update --project ../Booklify.Infrastructure --startup-project . --context ApplicationDbContext
   ```
   
   For BooklifyDbContext:
   ```
   dotnet ef database update --project ../Booklify.Infrastructure --startup-project . --context BooklifyDbContext
   ```

7. Run the application with one of these options:
   
   Standard run:
   ```
   dotnet run
   ```
   
   Run with hot reload (watch mode):
   ```
   dotnet watch run
   ```

## Development

- The solution follows Clean Architecture principles
- Domain-Driven Design (DDD) practices are used where applicable
- CQRS pattern is implemented for handling commands and queries
- Repository pattern is used for data access
- Dependency injection is used throughout the application

## Contributing

1. Create a feature branch
2. Commit your changes
3. Push to the branch
4. Create a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details 