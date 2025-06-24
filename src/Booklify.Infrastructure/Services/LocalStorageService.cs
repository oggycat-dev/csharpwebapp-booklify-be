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

        // Return relative path instead of full URL
        var relativePath = folder != null 
            ? Path.Combine(folder, uniqueFileName).Replace("\\", "/")
            : uniqueFileName;
            
        return relativePath; // Return relative path instead of full URL
    }

    public async Task<string> UploadFileAsync(byte[] fileBytes, string fileName, string contentType, string? folder = null)
    {
        using var stream = new MemoryStream(fileBytes);
        return await UploadFileAsync(stream, fileName, contentType, folder);
    }

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

    public async Task<string> UploadEpubFileAsync(byte[] fileBytes, string fileName, string contentType, string? categoryName = null)
    {
        using var stream = new MemoryStream(fileBytes);
        return await UploadEpubFileAsync(stream, fileName, contentType, categoryName);
    }

    public Task<bool> DeleteFileAsync(string fileUrlOrPath)
    {
        try
        {
            var filePath = GetFilePathFromUrl(fileUrlOrPath);
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

    public Task<bool> FileExistsAsync(string fileUrlOrPath)
    {
        try
        {
            var filePath = GetFilePathFromUrl(fileUrlOrPath);
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
        return $"{baseUrl}/{cleanPath}";
    }

    public async Task<Stream?> DownloadFileAsync(string fileUrlOrPath)
    {
        try
        {
            var filePath = GetFilePathFromUrl(fileUrlOrPath);
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

    public Task<StorageFileInfo?> GetFileInfoAsync(string fileUrlOrPath)
    {
        try
        {
            var filePath = GetFilePathFromUrl(fileUrlOrPath);
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
                    Url = fileUrlOrPath.StartsWith(_storageSettings.BaseUrl) ? fileUrlOrPath : GetFileUrl(fileUrlOrPath)
                });
            }
            return Task.FromResult<StorageFileInfo?>(null);
        }
        catch
        {
            return Task.FromResult<StorageFileInfo?>(null);
        }
    }

    private string GetFilePathFromUrl(string fileUrlOrPath)
    {
        // If it's already a relative path (no base URL), use it directly
        var baseUrl = _storageSettings.BaseUrl.TrimEnd('/');
        string relativePath;
        
        if (fileUrlOrPath.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
        {
            // It's a full URL, extract relative path
            relativePath = fileUrlOrPath.Substring(baseUrl.Length).TrimStart('/');
        }
        else
        {
            // It's already a relative path
            relativePath = fileUrlOrPath.TrimStart('/');
        }
        
        return Path.Combine(_uploadsPath, relativePath.Replace("/", "\\"));
    }

    /// <summary>
    /// Convert relative path to absolute file system path
    /// </summary>
    private string GetAbsoluteFilePath(string relativePath)
    {
        var cleanPath = relativePath.TrimStart('/');
        return Path.Combine(_uploadsPath, cleanPath.Replace("/", "\\"));
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
            ".epub" => "application/epub+zip",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Sanitize folder name for file system compatibility
    /// </summary>
    private string SanitizeFolderName(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return "uncategorized";

        // Remove invalid characters for file system
        var sanitized = folderName
            .Replace(" ", "-")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace("?", "")
            .Replace("<", "")
            .Replace(">", "")
            .Replace(":", "")
            .Replace("*", "")
            .Replace("|", "")
            .Replace("\"", "")
            .ToLowerInvariant();

        // Ensure it doesn't start or end with dash
        sanitized = sanitized.Trim('-');
        
        return string.IsNullOrEmpty(sanitized) ? "uncategorized" : sanitized;
    }
} 