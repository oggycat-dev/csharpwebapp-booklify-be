using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Booklify.Infrastructure.Utils;

/// <summary>
/// Utility class for sanitizing and generating unique file names
/// </summary>
public static class FileNameSanitizer
{
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
    private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();

    /// <summary>
    /// Create a unique file name by adding timestamp and ensuring it's safe while preserving original name
    /// </summary>
    public static string CreateUniqueFileName(string originalFileName)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            return $"file_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
        }

        var sanitizedFileName = SanitizeFileName(originalFileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitizedFileName);
        var extension = Path.GetExtension(sanitizedFileName);

        // Add timestamp and unique ID to make it unique while keeping original name readable
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var uniqueId = Guid.NewGuid().ToString("N")[..8]; // First 8 characters of GUID

        return $"{nameWithoutExtension}_{timestamp}_{uniqueId}{extension}";
    }

    /// <summary>
    /// Sanitize a file name by removing invalid characters and converting diacritics to ASCII
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "unknown_file";
        }

        // First, convert Vietnamese diacritics to ASCII equivalents
        fileName = RemoveDiacritics(fileName);

        // Remove invalid file name characters
        var sanitized = new StringBuilder();
        foreach (var c in fileName)
        {
            if (!InvalidFileNameChars.Contains(c) && !char.IsControl(c))
            {
                // Replace spaces with underscores for URL safety
                if (c == ' ')
                {
                    sanitized.Append('_');
                }
                else
                {
                    sanitized.Append(c);
                }
            }
            else
            {
                sanitized.Append('_');
            }
        }

        var result = sanitized.ToString();

        // Remove multiple consecutive underscores
        result = Regex.Replace(result, @"_{2,}", "_");

        // Remove leading/trailing underscores and dots
        result = result.Trim('_', '.');

        // Ensure it's not empty
        if (string.IsNullOrWhiteSpace(result))
        {
            result = "unknown_file";
        }

        // Limit length
        if (result.Length > 100)
        {
            var extension = Path.GetExtension(result);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(result);
            
            if (nameWithoutExtension.Length > 95)
            {
                nameWithoutExtension = nameWithoutExtension[..95];
            }
            
            result = nameWithoutExtension + extension;
        }

        return result;
    }

    /// <summary>
    /// Remove diacritics (accents) from characters to make them ASCII-compatible
    /// </summary>
    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // Normalize to decomposed form (NFD) - separates base characters from combining diacritics
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            // Get the Unicode category of the character
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            
            // Skip combining diacritical marks (accents)
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        // Convert back to composed form (NFC)
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Sanitize a directory path
    /// </summary>
    public static string SanitizeDirectoryPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        // Split path into parts and sanitize each part
        var parts = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        var sanitizedParts = parts.Select(SanitizeFileName).ToArray();

        return string.Join("/", sanitizedParts);
    }

    /// <summary>
    /// Check if a file name is valid
    /// </summary>
    public static bool IsValidFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        return !fileName.Any(c => InvalidFileNameChars.Contains(c) || char.IsControl(c));
    }
}