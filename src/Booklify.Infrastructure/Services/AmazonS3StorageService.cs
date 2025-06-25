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
        
        // Auto-determine folder based on file type if not specified
        if (folder == null)
        {
            folder = GetDefaultFolderForFileType(extension);
        }
        
        // Create S3 key with folder if specified
        var key = folder != null 
            ? $"{folder.Trim('/')}/{uniqueFileName}"
            : uniqueFileName;

        // Choose upload method based on file size
        if (fileStream.Length >= _storageSettings.AmazonS3.MultipartThreshold)
        {
            return await UploadLargeFileAsync(fileStream, key, contentType);
        }
        else
        {
            return await UploadSmallFileAsync(fileStream, key, contentType);
        }
    }
    
    /// <summary>
    /// Upload small files using regular PutObject
    /// </summary>
    private async Task<string> UploadSmallFileAsync(Stream fileStream, string key, string contentType)
    {
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
                return key; // Return relative path instead of full URL
            }
            
            throw new InvalidOperationException($"Failed to upload file to S3. Status: {response.HttpStatusCode}");
        }
        catch (AmazonS3Exception ex)
        {
            throw new InvalidOperationException($"S3 upload failed: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Upload large files using multipart upload
    /// </summary>
    private async Task<string> UploadLargeFileAsync(Stream fileStream, string key, string contentType)
    {
        var initiateRequest = new InitiateMultipartUploadRequest
        {
            BucketName = _storageSettings.AmazonS3.BucketName,
            Key = key,
            ContentType = contentType,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
            CannedACL = S3CannedACL.PublicRead
        };

        try
        {
            var initiateResponse = await _s3Client.InitiateMultipartUploadAsync(initiateRequest);
            var uploadId = initiateResponse.UploadId;
            
            var partETags = new List<PartETag>();
            var partSize = _storageSettings.AmazonS3.PartSize;
            var fileSize = fileStream.Length;
            var partNumber = 1;
            var tasks = new List<Task<PartETag>>();
            var semaphore = new SemaphoreSlim(_storageSettings.AmazonS3.MaxConcurrentParts);

            for (long currentPosition = 0; currentPosition < fileSize; currentPosition += partSize)
            {
                var currentPartSize = Math.Min(partSize, fileSize - currentPosition);
                var partData = new byte[currentPartSize];
                
                fileStream.Position = currentPosition;
                await fileStream.ReadAsync(partData, 0, (int)currentPartSize);
                
                var partNum = partNumber++;
                
                // Use semaphore to limit concurrent uploads
                await semaphore.WaitAsync();
                
                var task = UploadPartAsync(partData, partNum, uploadId, key, semaphore);
                tasks.Add(task);
            }

            // Wait for all parts to complete
            var completedParts = await Task.WhenAll(tasks);
            partETags.AddRange(completedParts);

            // Complete the multipart upload
            var completeRequest = new CompleteMultipartUploadRequest
            {
                BucketName = _storageSettings.AmazonS3.BucketName,
                Key = key,
                UploadId = uploadId,
                PartETags = partETags.OrderBy(p => p.PartNumber).ToList()
            };

            var completeResponse = await _s3Client.CompleteMultipartUploadAsync(completeRequest);
            
            if (completeResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                return key; // Return relative path instead of full URL
            }
            
            throw new InvalidOperationException($"Failed to complete multipart upload. Status: {completeResponse.HttpStatusCode}");
        }
        catch (AmazonS3Exception ex)
        {
            // Try to abort the multipart upload if it fails
            try
            {
                var abortRequest = new AbortMultipartUploadRequest
                {
                    BucketName = _storageSettings.AmazonS3.BucketName,
                    Key = key,
                    UploadId = ex.Message.Contains("UploadId") ? ex.Message : string.Empty
                };
                await _s3Client.AbortMultipartUploadAsync(abortRequest);
            }
            catch
            {
                // Ignore abort errors
            }
            
            throw new InvalidOperationException($"S3 multipart upload failed: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Upload a single part of a multipart upload
    /// </summary>
    private async Task<PartETag> UploadPartAsync(byte[] partData, int partNumber, string uploadId, string key, SemaphoreSlim semaphore)
    {
        try
        {
            using var partStream = new MemoryStream(partData);
            
            var uploadPartRequest = new UploadPartRequest
            {
                BucketName = _storageSettings.AmazonS3.BucketName,
                Key = key,
                UploadId = uploadId,
                PartNumber = partNumber,
                InputStream = partStream,
                PartSize = partData.Length
            };

            var response = await _s3Client.UploadPartAsync(uploadPartRequest);
            
            return new PartETag(partNumber, response.ETag);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<string> UploadFileAsync(byte[] fileBytes, string fileName, string contentType, string? folder = null)
    {
        using var stream = new MemoryStream(fileBytes);
        return await UploadFileAsync(stream, fileName, contentType, folder);
    }

    /// <summary>
    /// Upload EPUB file with specific book category prefix
    /// </summary>
    public async Task<string> UploadEpubFileAsync(Stream fileStream, string fileName, string contentType, string? categoryName = null)
    {
        // Validate that this is an EPUB file
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension != ".epub")
        {
            throw new InvalidOperationException("This method is only for EPUB files");
        }

        // Create folder structure: books/epub/{category}/
        var folder = categoryName != null 
            ? $"books/epub/{SanitizeFolderName(categoryName)}"
            : "books/epub";

        return await UploadFileAsync(fileStream, fileName, contentType, folder);
    }

    /// <summary>
    /// Upload EPUB file with specific book category prefix (byte array version)
    /// </summary>
    public async Task<string> UploadEpubFileAsync(byte[] fileBytes, string fileName, string contentType, string? categoryName = null)
    {
        using var stream = new MemoryStream(fileBytes);
        return await UploadEpubFileAsync(stream, fileName, contentType, categoryName);
    }

    public async Task<bool> DeleteFileAsync(string fileUrlOrPath)
    {
        try
        {
            var key = GetKeyFromUrl(fileUrlOrPath);
            
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

    public async Task<bool> FileExistsAsync(string fileUrlOrPath)
    {
        try
        {
            var key = GetKeyFromUrl(fileUrlOrPath);
            
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
        // Use configured BaseUrl if available, otherwise fallback to S3 URL generation
        if (!string.IsNullOrEmpty(_storageSettings.BaseUrl))
        {
            var baseUrl = _storageSettings.BaseUrl.TrimEnd('/');
            var relativePath = filePath.TrimStart('/');
            return $"{baseUrl}/{relativePath}";
        }
        
        // Fallback to S3 URL generation if BaseUrl is not configured
        var bucketName = _storageSettings.AmazonS3.BucketName;
        var region = _storageSettings.AmazonS3.Region;
        var protocol = _storageSettings.AmazonS3.UseHttps ? "https" : "http";
        var cleanPath = filePath.TrimStart('/');
        
        return $"{protocol}://{bucketName}.s3.{region}.amazonaws.com/{cleanPath}";
    }

    public async Task<Stream?> DownloadFileAsync(string fileUrlOrPath)
    {
        try
        {
            var key = GetKeyFromUrl(fileUrlOrPath);
            
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

    public async Task<StorageFileInfo?> GetFileInfoAsync(string fileUrlOrPath)
    {
        try
        {
            var key = GetKeyFromUrl(fileUrlOrPath);
            
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
                Url = IsFullUrl(fileUrlOrPath) ? fileUrlOrPath : GetFileUrl(fileUrlOrPath)
            };
        }
        catch (AmazonS3Exception)
        {
            return null;
        }
    }

    private string GetKeyFromUrl(string fileUrlOrPath)
    {
        // Check if it matches the configured BaseUrl first
        if (!string.IsNullOrEmpty(_storageSettings.BaseUrl) && 
            fileUrlOrPath.StartsWith(_storageSettings.BaseUrl, StringComparison.OrdinalIgnoreCase))
        {
            var baseUrl = _storageSettings.BaseUrl.TrimEnd('/');
            return fileUrlOrPath.Substring(baseUrl.Length + 1); // +1 for the trailing slash
        }
        
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
            if (fileUrlOrPath.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return fileUrlOrPath.Substring(pattern.Length);
            }
        }

        // If no pattern matches, assume it's already a relative path (key)
        return fileUrlOrPath.TrimStart('/');
    }

    /// <summary>
    /// Get default folder based on file extension
    /// </summary>
    private string? GetDefaultFolderForFileType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".epub" => "books/epub",
            ".pdf" => "books/pdf", 
            ".jpg" or ".jpeg" => "images/covers",
            ".png" => "images/covers",
            ".webp" => "images/covers",
            ".gif" => "images/covers",
            ".mp3" => "audio/books",
            ".wav" => "audio/books",
            ".mp4" => "video/books",
            ".avi" => "video/books",
            ".txt" => "documents/text",
            ".docx" => "documents/word",
            ".doc" => "documents/word",
            _ => "uploads" // Default folder for other file types
        };
    }

    /// <summary>
    /// Sanitize folder name for S3 compatibility
    /// </summary>
    private string SanitizeFolderName(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return "uncategorized";

        // Remove invalid characters for S3 keys
        var sanitized = folderName
            .Replace(" ", "-")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace("?", "")
            .Replace("#", "")
            .Replace("[", "")
            .Replace("]", "")
            .Replace("@", "")
            .Replace("!", "")
            .Replace("$", "")
            .Replace("&", "")
            .Replace("'", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("*", "")
            .Replace("+", "")
            .Replace(",", "")
            .Replace(";", "")
            .Replace("=", "")
            .Replace(":", "")
            .ToLowerInvariant();

        // Ensure it doesn't start or end with dash
        sanitized = sanitized.Trim('-');
        
        return string.IsNullOrEmpty(sanitized) ? "uncategorized" : sanitized;
    }

    private bool IsFullUrl(string fileUrl)
    {
        // Check if it matches the configured BaseUrl
        if (!string.IsNullOrEmpty(_storageSettings.BaseUrl) && 
            fileUrl.StartsWith(_storageSettings.BaseUrl, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
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
                return true;
            }
        }

        return false;
    }
}
