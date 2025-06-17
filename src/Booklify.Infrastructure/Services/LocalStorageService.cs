using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Booklify.Application.Common.Interfaces;
using Booklify.Infrastructure.Models;

namespace Booklify.Infrastructure.Services;

/// <summary>
/// Local file storage service implementation
/// </summary>
public class LocalStorageService : IStorageService
{
    private readonly StorageSettings _storageSettings;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly string _uploadsPath;

    public LocalStorageService(IOptions<StorageSettings> storageSettings, IWebHostEnvironment webHostEnvironment)
    {
        _storageSettings = storageSettings.Value;
        _webHostEnvironment = webHostEnvironment;
        _uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath ?? _webHostEnvironment.ContentRootPath, 
            _storageSettings.LocalStorage.RootPath.Replace("wwwroot/", ""));
        
        // Ensure the uploads directory exists
        if (!Directory.Exists(_uploadsPath))
        {
            Directory.CreateDirectory(_uploadsPath);
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string? folder = null)
    {
        // Validate file extension
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!_storageSettings.LocalStorage.AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"File extension {extension} is not allowed");
        }

        // Validate file size
        if (fileStream.Length > _storageSettings.LocalStorage.MaxFileSize)
        {
            throw new InvalidOperationException($"File size exceeds maximum allowed size of {_storageSettings.LocalStorage.MaxFileSize} bytes");
        }

        // Generate unique filename
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        
        // Create folder path if specified
        string targetDirectory = _uploadsPath;
        if (!string.IsNullOrEmpty(folder))
        {
            targetDirectory = Path.Combine(_uploadsPath, folder);
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }
        }

        var filePath = Path.Combine(targetDirectory, uniqueFileName);

        // Save file
        using var fileStreamOutput = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(fileStreamOutput);

        // Return relative path for URL generation
        var relativePath = folder != null 
            ? Path.Combine(folder, uniqueFileName).Replace("\\", "/")
            : uniqueFileName;
            
        return GetFileUrl(relativePath);
    }

    public async Task<string> UploadFileAsync(byte[] fileBytes, string fileName, string contentType, string? folder = null)
    {
        using var stream = new MemoryStream(fileBytes);
        return await UploadFileAsync(stream, fileName, contentType, folder);
    }

    public Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            var filePath = GetFilePathFromUrl(fileUrl);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> FileExistsAsync(string fileUrl)
    {
        try
        {
            var filePath = GetFilePathFromUrl(fileUrl);
            return Task.FromResult(File.Exists(filePath));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public string GetFileUrl(string filePath)
    {
        var baseUrl = _storageSettings.BaseUrl.TrimEnd('/');
        var cleanPath = filePath.TrimStart('/').Replace("\\", "/");
        return $"{baseUrl}/uploads/{cleanPath}";
    }

    public async Task<Stream?> DownloadFileAsync(string fileUrl)
    {
        try
        {
            var filePath = GetFilePathFromUrl(fileUrl);
            if (File.Exists(filePath))
            {
                var fileBytes = await File.ReadAllBytesAsync(filePath);
                return new MemoryStream(fileBytes);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public Task<StorageFileInfo?> GetFileInfoAsync(string fileUrl)
    {
        try
        {
            var filePath = GetFilePathFromUrl(fileUrl);
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                return Task.FromResult<StorageFileInfo?>(new StorageFileInfo
                {
                    FileName = fileInfo.Name,
                    Size = fileInfo.Length,
                    ContentType = GetContentType(fileInfo.Extension),
                    CreatedAt = fileInfo.CreationTimeUtc,
                    LastModified = fileInfo.LastWriteTimeUtc,
                    Url = fileUrl
                });
            }
            return Task.FromResult<StorageFileInfo?>(null);
        }
        catch
        {
            return Task.FromResult<StorageFileInfo?>(null);
        }
    }

    private string GetFilePathFromUrl(string fileUrl)
    {
        // Extract relative path from URL
        var baseUrl = _storageSettings.BaseUrl.TrimEnd('/');
        var relativePath = fileUrl.Replace(baseUrl, "").Replace("/uploads/", "").TrimStart('/');
        return Path.Combine(_uploadsPath, relativePath.Replace("/", "\\"));
    }

    private static string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
} 