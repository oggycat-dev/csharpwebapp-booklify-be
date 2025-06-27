namespace Booklify.Infrastructure.Models;

/// <summary>
/// Storage settings configuration
/// </summary>
public class StorageSettings
{
    public string ProviderType { get; set; } = "LocalStorage";
    public string BaseUrl { get; set; } = string.Empty;
    public LocalStorageSettings LocalStorage { get; set; } = new();
    public AmazonS3Settings AmazonS3 { get; set; } = new();
    public AzureBlobSettings AzureBlob { get; set; } = new();
}

/// <summary>
/// Local storage specific settings
/// </summary>
public class LocalStorageSettings
{
    public string RootPath { get; set; } = "wwwroot/uploads";
    public long MaxFileSize { get; set; } = 500 * 1024 * 1024; // 500MB
    public string[] AllowedExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx", ".epub", ".mp4", ".avi", ".mov", ".mkv", ".zip", ".rar" };
}

/// <summary>
/// Amazon S3 specific settings
/// </summary>
public class AmazonS3Settings
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public bool UseHttps { get; set; } = true;
    public long MaxFileSize { get; set; } = 500 * 1024 * 1024; // 500MB
    public string[] AllowedExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx", ".epub", ".mp4", ".avi", ".mov", ".mkv", ".zip", ".rar" };
    
    // Multipart upload settings
    public long MultipartThreshold { get; set; } = 100 * 1024 * 1024; // 100MB - files larger than this use multipart
    public long PartSize { get; set; } = 10 * 1024 * 1024; // 10MB per part
    public int MaxConcurrentParts { get; set; } = 5; // Max concurrent uploads
}

/// <summary>
/// Azure Blob storage specific settings
/// </summary>
public class AzureBlobSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public long MaxFileSize { get; set; } = 500 * 1024 * 1024; // 500MB
    public string[] AllowedExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx", ".epub", ".mp4", ".avi", ".mov", ".mkv", ".zip", ".rar" };
} 