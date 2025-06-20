using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Booklify.Application.Common.Interfaces;
using Booklify.Infrastructure.Models;

namespace Booklify.Infrastructure.Services;

/// <summary>
/// Amazon S3 storage service implementation
/// </summary>
public class AmazonS3StorageService : IStorageService
{
    private readonly StorageSettings _storageSettings;
    private readonly IAmazonS3 _s3Client;

    public AmazonS3StorageService(IOptions<StorageSettings> storageSettings, IAmazonS3 s3Client)
    {
        _storageSettings = storageSettings.Value;
        _s3Client = s3Client;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string? folder = null)
    {
        // Validate file extension
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!_storageSettings.AmazonS3.AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"File extension {extension} is not allowed");
        }

        // Validate file size
        if (fileStream.Length > _storageSettings.AmazonS3.MaxFileSize)
        {
            throw new InvalidOperationException($"File size exceeds maximum allowed size of {_storageSettings.AmazonS3.MaxFileSize} bytes");
        }

        // Generate unique filename
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        
        // Create S3 key with folder if specified
        var key = folder != null 
            ? $"{folder.Trim('/')}/{uniqueFileName}"
            : uniqueFileName;

        var request = new PutObjectRequest
        {
            BucketName = _storageSettings.AmazonS3.BucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
            CannedACL = S3CannedACL.PublicRead
        };

        try
        {
            var response = await _s3Client.PutObjectAsync(request);
            
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                return GetFileUrl(key);
            }
            
            throw new InvalidOperationException($"Failed to upload file to S3. Status: {response.HttpStatusCode}");
        }
        catch (AmazonS3Exception ex)
        {
            throw new InvalidOperationException($"S3 upload failed: {ex.Message}", ex);
        }
    }

    public async Task<string> UploadFileAsync(byte[] fileBytes, string fileName, string contentType, string? folder = null)
    {
        using var stream = new MemoryStream(fileBytes);
        return await UploadFileAsync(stream, fileName, contentType, folder);
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            var key = GetKeyFromUrl(fileUrl);
            
            var request = new DeleteObjectRequest
            {
                BucketName = _storageSettings.AmazonS3.BucketName,
                Key = key
            };

            var response = await _s3Client.DeleteObjectAsync(request);
            return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
        }
        catch (AmazonS3Exception)
        {
            return false;
        }
    }

    public async Task<bool> FileExistsAsync(string fileUrl)
    {
        try
        {
            var key = GetKeyFromUrl(fileUrl);
            
            var request = new GetObjectMetadataRequest
            {
                BucketName = _storageSettings.AmazonS3.BucketName,
                Key = key
            };

            await _s3Client.GetObjectMetadataAsync(request);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    public string GetFileUrl(string filePath)
    {
        var bucketName = _storageSettings.AmazonS3.BucketName;
        var region = _storageSettings.AmazonS3.Region;
        var protocol = _storageSettings.AmazonS3.UseHttps ? "https" : "http";
        var cleanPath = filePath.TrimStart('/');
        
        return $"{protocol}://{bucketName}.s3.{region}.amazonaws.com/{cleanPath}";
    }

    public async Task<Stream?> DownloadFileAsync(string fileUrl)
    {
        try
        {
            var key = GetKeyFromUrl(fileUrl);
            
            var request = new GetObjectRequest
            {
                BucketName = _storageSettings.AmazonS3.BucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectAsync(request);
            
            // Copy to memory stream to ensure the stream can be read multiple times
            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            
            return memoryStream;
        }
        catch (AmazonS3Exception)
        {
            return null;
        }
    }

    public async Task<StorageFileInfo?> GetFileInfoAsync(string fileUrl)
    {
        try
        {
            var key = GetKeyFromUrl(fileUrl);
            
            var request = new GetObjectMetadataRequest
            {
                BucketName = _storageSettings.AmazonS3.BucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectMetadataAsync(request);
            
            return new StorageFileInfo
            {
                FileName = Path.GetFileName(key),
                Size = response.ContentLength,
                ContentType = response.Headers.ContentType,
                CreatedAt = response.LastModified ?? DateTime.UtcNow,
                LastModified = response.LastModified ?? DateTime.UtcNow,
                Url = fileUrl
            };
        }
        catch (AmazonS3Exception)
        {
            return null;
        }
    }

    private string GetKeyFromUrl(string fileUrl)
    {
        var bucketName = _storageSettings.AmazonS3.BucketName;
        var region = _storageSettings.AmazonS3.Region;
        
        // Handle different S3 URL formats
        var patterns = new[]
        {
            $"https://{bucketName}.s3.{region}.amazonaws.com/",
            $"http://{bucketName}.s3.{region}.amazonaws.com/",
            $"https://s3.{region}.amazonaws.com/{bucketName}/",
            $"http://s3.{region}.amazonaws.com/{bucketName}/"
        };

        foreach (var pattern in patterns)
        {
            if (fileUrl.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return fileUrl.Substring(pattern.Length);
            }
        }

        // If no pattern matches, assume it's already a key
        return fileUrl.TrimStart('/');
    }
} 