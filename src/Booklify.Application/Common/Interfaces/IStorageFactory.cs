namespace Booklify.Application.Common.Interfaces;

/// <summary>
/// Factory interface for creating storage service instances
/// </summary>
public interface IStorageFactory
{
    /// <summary>
    /// Create a storage service instance based on the configured provider
    /// </summary>
    /// <returns>Storage service instance</returns>
    IStorageService CreateStorageService();
}

/// <summary>
/// Storage provider types
/// </summary>
public enum StorageProviderType
{
    LocalStorage,
    AmazonS3,
    AzureBlob
} 