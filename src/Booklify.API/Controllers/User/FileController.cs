using Microsoft.AspNetCore.Mvc;
using Booklify.Application.Common.Interfaces;

namespace Booklify.API.Controllers.User;

/// <summary>
/// File management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    private readonly IStorageService _storageService;
    private readonly ILogger<FileController> _logger;

    public FileController(IStorageService storageService, ILogger<FileController> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a file
    /// </summary>
    /// <param name="file">File to upload</param>
    /// <param name="folder">Optional folder path</param>
    /// <returns>File URL</returns>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery] string? folder = null)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            using var stream = file.OpenReadStream();
            var fileUrl = await _storageService.UploadFileAsync(stream, file.FileName, file.ContentType, folder);

            return Ok(new { url = fileUrl, fileName = file.FileName });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "File upload validation failed: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (NotImplementedException ex)
        {
            _logger.LogError(ex, "Storage service not properly configured: {Message}", ex.Message);
            return StatusCode(501, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, "Internal server error while uploading file");
        }
    }

    /// <summary>
    /// Upload EPUB file with automatic categorization
    /// </summary>
    /// <param name="file">EPUB file to upload</param>
    /// <param name="categoryName">Optional category name for folder structure</param>
    /// <returns>File URL</returns>
    [HttpPost("upload-epub")]
    public async Task<IActionResult> UploadEpubFile(IFormFile file, [FromQuery] string? categoryName = null)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            // Validate EPUB file
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".epub")
                return BadRequest("Only EPUB files are allowed for this endpoint");

            using var stream = file.OpenReadStream();
            var fileUrl = await _storageService.UploadEpubFileAsync(stream, file.FileName, file.ContentType, categoryName);

            return Ok(new 
            { 
                url = fileUrl, 
                fileName = file.FileName,
                category = categoryName ?? "uncategorized",
                fileType = "epub"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "EPUB file upload validation failed: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (NotImplementedException ex)
        {
            _logger.LogError(ex, "Storage service not properly configured: {Message}", ex.Message);
            return StatusCode(501, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading EPUB file");
            return StatusCode(500, "Internal server error while uploading EPUB file");
        }
    }

    /// <summary>
    /// Delete a file
    /// </summary>
    /// <param name="fileUrl">URL of the file to delete</param>
    /// <returns>Success status</returns>
    [HttpDelete]
    public async Task<IActionResult> DeleteFile([FromQuery] string fileUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(fileUrl))
                return BadRequest("File URL is required");

            var success = await _storageService.DeleteFileAsync(fileUrl);
            return Ok(new { success });
        }
        catch (NotImplementedException ex)
        {
            _logger.LogError(ex, "Storage service not properly configured: {Message}", ex.Message);
            return StatusCode(501, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file");
            return StatusCode(500, "Internal server error while deleting file");
        }
    }

    /// <summary>
    /// Check if file exists
    /// </summary>
    /// <param name="fileUrl">URL of the file to check</param>
    /// <returns>Existence status</returns>
    [HttpGet("exists")]
    public async Task<IActionResult> FileExists([FromQuery] string fileUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(fileUrl))
                return BadRequest("File URL is required");

            var exists = await _storageService.FileExistsAsync(fileUrl);
            return Ok(new { exists });
        }
        catch (NotImplementedException ex)
        {
            _logger.LogError(ex, "Storage service not properly configured: {Message}", ex.Message);
            return StatusCode(501, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence");
            return StatusCode(500, "Internal server error while checking file");
        }
    }

    /// <summary>
    /// Get file information
    /// </summary>
    /// <param name="fileUrl">URL of the file</param>
    /// <returns>File information</returns>
    [HttpGet("info")]
    public async Task<IActionResult> GetFileInfo([FromQuery] string fileUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(fileUrl))
                return BadRequest("File URL is required");

            var fileInfo = await _storageService.GetFileInfoAsync(fileUrl);
            if (fileInfo == null)
                return NotFound("File not found");

            return Ok(fileInfo);
        }
        catch (NotImplementedException ex)
        {
            _logger.LogError(ex, "Storage service not properly configured: {Message}", ex.Message);
            return StatusCode(501, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file info");
            return StatusCode(500, "Internal server error while getting file info");
        }
    }

    /// <summary>
    /// Download a file
    /// </summary>
    /// <param name="fileUrl">URL of the file to download</param>
    /// <returns>File stream</returns>
    [HttpGet("download")]
    public async Task<IActionResult> DownloadFile([FromQuery] string fileUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(fileUrl))
                return BadRequest("File URL is required");

            var stream = await _storageService.DownloadFileAsync(fileUrl);
            if (stream == null)
                return NotFound("File not found");

            var fileInfo = await _storageService.GetFileInfoAsync(fileUrl);
            var fileName = fileInfo?.FileName ?? Path.GetFileName(fileUrl);
            var contentType = fileInfo?.ContentType ?? "application/octet-stream";

            return File(stream, contentType, fileName);
        }
        catch (NotImplementedException ex)
        {
            _logger.LogError(ex, "Storage service not properly configured: {Message}", ex.Message);
            return StatusCode(501, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file");
            return StatusCode(500, "Internal server error while downloading file");
        }
    }
} 