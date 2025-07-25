# Storage Service Implementation

This document describes the storage service implementation using the factory pattern that supports multiple storage providers.

## Overview

The storage service is implemented using the factory pattern and supports switching between different storage providers through environment configuration. Currently supported providers:

- **LocalStorage**: Stores files locally in the application's file system
- **AmazonS3**: Stores files in Amazon S3 buckets (requires AWS SDK)

## Architecture

### Interfaces
- `IStorageService`: Main interface for storage operations
- `IStorageFactory`: Factory interface for creating storage service instances

### Implementations
- `LocalStorageService`: Local file system storage implementation
- `AmazonS3StorageService`: Amazon S3 storage implementation (requires AWSSDK.S3 package)
- `StorageFactory`: Factory that creates appropriate storage service based on configuration

### Configuration
- `StorageSettings`: Configuration model for storage settings
- Environment variables are mapped through `EnvironmentConfiguration`

## Environment Configuration

Set these environment variables to configure the storage service:

### Required Settings
```bash
# Storage provider type (LocalStorage, AmazonS3, AzureBlob)
STORAGE_PROVIDER_TYPE=LocalStorage
# Base URL for serving files
STORAGE_BASE_URL=http://localhost:5123
```

### Local Storage Settings
```bash
# Path relative to wwwroot or content root
LOCAL_STORAGE_ROOT_PATH=wwwroot/uploads
# Max file size in bytes (10MB = 10485760)
LOCAL_STORAGE_MAX_FILE_SIZE=10485760
```

### Amazon S3 Settings
```bash
# AWS Credentials
AWS_ACCESS_KEY_ID=your_access_key_here
AWS_SECRET_ACCESS_KEY=your_secret_key_here
# S3 Bucket Configuration
AWS_S3_BUCKET_NAME=your-bucket-name
AWS_S3_REGION=us-east-1
AWS_S3_USE_HTTPS=true
# Max file size in bytes (50MB = 52428800)
AWS_S3_MAX_FILE_SIZE=52428800
```

## Usage

### Dependency Injection
The storage service is automatically registered in the DI container and can be injected as `IStorageService`:

```csharp
public class MyController : ControllerBase
{
    private readonly IStorageService _storageService;
    
    public MyController(IStorageService storageService)
    {
        _storageService = storageService;
    }
}
```

### File Operations

#### Upload File
```csharp
// Upload from stream
var fileUrl = await _storageService.UploadFileAsync(fileStream, fileName, contentType, folder);

// Upload from byte array
var fileUrl = await _storageService.UploadFileAsync(fileBytes, fileName, contentType, folder);
```

#### Delete File
```csharp
var success = await _storageService.DeleteFileAsync(fileUrl);
```

#### Check File Existence
```csharp
var exists = await _storageService.FileExistsAsync(fileUrl);
```

#### Download File
```csharp
var stream = await _storageService.DownloadFileAsync(fileUrl);
```

#### Get File Information
```csharp
var fileInfo = await _storageService.GetFileInfoAsync(fileUrl);
```

#### Get File URL
```csharp
var publicUrl = _storageService.GetFileUrl(filePath);
```

## API Endpoints

The `FileController` provides REST endpoints for file operations:

- `POST /api/file/upload` - Upload a file
- `DELETE /api/file?fileUrl={url}` - Delete a file
- `GET /api/file/exists?fileUrl={url}` - Check if file exists
- `GET /api/file/info?fileUrl={url}` - Get file information
- `GET /api/file/download?fileUrl={url}` - Download a file

## Setup Instructions

### For Local Storage
1. Set `STORAGE_PROVIDER_TYPE=LocalStorage`
2. Configure `STORAGE_BASE_URL` to your application's base URL
3. Optionally configure `LOCAL_STORAGE_ROOT_PATH` and `LOCAL_STORAGE_MAX_FILE_SIZE`

### For Amazon S3
1. Install AWS SDK: `dotnet add package AWSSDK.S3`
2. Uncomment the S3 service implementation in `AmazonS3StorageService.cs`
3. Update dependency injection to include AWS S3 client
4. Set `STORAGE_PROVIDER_TYPE=AmazonS3`
5. Configure AWS credentials and S3 settings

## Security Considerations

- File extension validation is enforced based on configuration
- File size limits are enforced based on configuration
- For S3: Files are uploaded with server-side encryption (AES256)
- For S3: Files are made publicly readable by default (consider changing for private files)

## Error Handling

The service throws appropriate exceptions for various error conditions:
- `InvalidOperationException`: For validation errors (file size, extension, upload failures)
- `NotImplementedException`: When required dependencies are not installed
- Standard exceptions are caught and logged by the controllers

## Adding New Storage Providers

To add a new storage provider:

1. Create a new service class implementing `IStorageService`
2. Add configuration settings to `StorageSettings`
3. Update `EnvironmentConfiguration` to map environment variables
4. Update `StorageFactory` to handle the new provider type
5. Register the new service in `DependencyInjection`

## File Structure

```
src/
├── Booklify.Application/
│   └── Common/
│       └── Interfaces/
│           ├── IStorageService.cs
│           └── IStorageFactory.cs
├── Booklify.Infrastructure/
│   ├── Models/
│   │   └── StorageSettings.cs
│   └── Services/
│       ├── LocalStorageService.cs
│       ├── AmazonS3StorageService.cs
│       └── StorageFactory.cs
├── Booklify.API/
│   ├── Controllers/
│   │   └── FileController.cs
│   └── Configurations/
│       └── EnvironmentConfiguration.cs
└── storage-example.env 