using Microsoft.Extensions.Logging;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Domain.Commons;
using Booklify.Domain.Entities;
using Booklify.Domain.Enums;
using Booklify.Infrastructure.Models;
using VersOne.Epub;
using VersOne.Epub.Schema;

namespace Booklify.Infrastructure.Services;

public class EPubService : IEPubService
{
    private readonly IFileBackgroundService _fileBackgroundService;
    private readonly ILogger<EPubService> _logger;

    public EPubService(IFileBackgroundService fileBackgroundService, ILogger<EPubService> logger)
    {
        _fileBackgroundService = fileBackgroundService;
        _logger = logger;
    }

    public async Task<(List<Chapter> chapters, int totalCount)> ExtractChapters(string epubFilePath)
    {
        try
        {
            var epubBook = await EpubReader.ReadBookAsync(epubFilePath);
            var chapters = new List<Chapter>();
            var currentPart = (Chapter?)null;
            var order = 1;

            // Create a lookup for navigation items by file path
            _logger.LogDebug("EPUB Navigation items count: {Count}", epubBook.Navigation?.Count ?? 0);
            var navigationLookup = CreateNavigationLookup(epubBook.Navigation ?? new List<EpubNavigationItem>());
            _logger.LogDebug("Navigation lookup created with {Count} entries", navigationLookup.Count);

            // Process items in reading order to maintain proper hierarchy
            foreach (var item in epubBook.ReadingOrder)
            {
                var fileName = Path.GetFileNameWithoutExtension(item.FilePath);
                
                // Skip system files
                if (IsSystemFile(fileName))
                {
                    continue;
                }

                // Get the real title from navigation, or use a fallback
                var realTitle = GetRealTitle(item.FilePath, navigationLookup);
                
                // If no navigation title found, try to extract from HTML content
                if (string.IsNullOrWhiteSpace(realTitle))
                {
                    realTitle = TryExtractTitleFromHtml(epubBook, item.FilePath);
                }

                // Check if it's a part (for structured EPUBs like "Once a Week")
                var partMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"^pt(\d+)$");
                if (partMatch.Success)
                {
                    var partNumber = int.Parse(partMatch.Groups[1].Value);
                    var part = new Chapter
                    {
                        Title = realTitle ?? $"Part {partNumber}",
                        Order = order++,
                        Href = item.FilePath,
                        ParentChapterId = null
                    };
                    BaseEntityExtensions.InitializeBaseEntity(part, (Guid?)null);
                    chapters.Add(part);
                    currentPart = part;
                    _logger.LogDebug("Created part {PartNumber} with title '{Title}' and ID {PartId}", 
                        partNumber, part.Title, part.Id);
                    continue;
                }

                // Check if it's a chapter with ch prefix (for structured EPUBs)
                var chapterMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"^ch(\d+)$");
                if (chapterMatch.Success)
                {
                    var chapterNumber = int.Parse(chapterMatch.Groups[1].Value);
                    var chapter = new Chapter
                    {
                        Title = realTitle ?? $"Chapter {chapterNumber}",
                        Order = order++,
                        Href = item.FilePath,
                        ParentChapterId = currentPart?.Id
                    };
                    BaseEntityExtensions.InitializeBaseEntity(chapter, (Guid?)null);
                    chapters.Add(chapter);

                    _logger.LogDebug("Created chapter {ChapterNumber} with title '{Title}' under part {PartTitle}", 
                        chapterNumber, chapter.Title, currentPart?.Title ?? "None");
                    continue;
                }

                // Check if it's a section (like ch14s01, ch14s02)
                var sectionMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"^ch(\d+)s(\d+)$");
                if (sectionMatch.Success)
                {
                    var chapterNumber = int.Parse(sectionMatch.Groups[1].Value);
                    var sectionNumber = int.Parse(sectionMatch.Groups[2].Value);
                    
                    // Find the parent chapter
                    var parentChapter = chapters.LastOrDefault(c => c.Title.Contains($"Chapter {chapterNumber}") || 
                                                                   c.Href?.Contains($"ch{chapterNumber}") == true);
                    
                    var section = new Chapter
                    {
                        Title = realTitle ?? $"Chapter {chapterNumber} Section {sectionNumber}",
                        Order = order++,
                        Href = item.FilePath,
                        ParentChapterId = parentChapter?.Id
                    };
                    BaseEntityExtensions.InitializeBaseEntity(section, (Guid?)null);
                    chapters.Add(section);

                    _logger.LogDebug("Created section {ChapterNumber}.{SectionNumber} with title '{Title}' under chapter {ParentTitle}", 
                        chapterNumber, sectionNumber, section.Title, parentChapter?.Title ?? "None");
                    continue;
                }

                // Check if it's a simple numbered file (for simple EPUBs like "Khai Cuộc Mưu Lược")
                var simpleNumberMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"^(\d+)$");
                if (simpleNumberMatch.Success)
                {
                    var chapterNumber = int.Parse(simpleNumberMatch.Groups[1].Value);
                    var chapter = new Chapter
                    {
                        Title = realTitle ?? $"Chapter {chapterNumber}",
                        Order = order++,
                        Href = item.FilePath,
                        ParentChapterId = null // Simple EPUBs don't have parts
                    };
                    BaseEntityExtensions.InitializeBaseEntity(chapter, (Guid?)null);
                    chapters.Add(chapter);

                    _logger.LogDebug("Created simple chapter {ChapterNumber} with title '{Title}'", 
                        chapterNumber, chapter.Title);
                    continue;
                }

                // Handle any other files that might be chapters (fallback)
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    var chapter = new Chapter
                    {
                        Title = realTitle ?? fileName,
                        Order = order++,
                        Href = item.FilePath,
                        ParentChapterId = currentPart?.Id
                    };
                    BaseEntityExtensions.InitializeBaseEntity(chapter, (Guid?)null);
                    chapters.Add(chapter);

                    _logger.LogDebug("Created fallback chapter with title '{Title}'", chapter.Title);
                }
            }

            _logger.LogInformation("Successfully extracted {ChapterCount} chapters with proper hierarchy", 
                chapters.Count);
            
            return (chapters, chapters.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract chapters from EPUB file: {FilePath}", epubFilePath);
            throw;
        }
    }

    private bool IsSystemFile(string fileName)
    {
        return fileName.StartsWith("cover") ||
               fileName.StartsWith("index") ||
               fileName.StartsWith("toc") ||
               fileName.StartsWith("bk01-toc") ||
               fileName.StartsWith("pr01") ||
               fileName.StartsWith("nav") ||
               fileName.Equals("titlepage") ||
               fileName.Equals("copyright") ||
               fileName.Equals("dedication") ||
               fileName.Equals("preface") ||
               fileName.Equals("acknowledgments");
    }

    /// <summary>
    /// Creates a lookup dictionary for navigation items by their file paths
    /// </summary>
    private Dictionary<string, string> CreateNavigationLookup(IList<EpubNavigationItem> navigationItems)
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        void ProcessNavigationItems(IList<EpubNavigationItem> items)
        {
            foreach (var navItem in items)
            {
                _logger.LogDebug("Processing nav item: Title='{Title}', Link='{Link}', Type='{Type}'", 
                    navItem.Title ?? "NULL", 
                    navItem.Link?.ContentFilePath ?? "NULL", 
                    navItem.Type);

                if (!string.IsNullOrWhiteSpace(navItem.Link?.ContentFilePath) && 
                    !string.IsNullOrWhiteSpace(navItem.Title))
                {
                    // Remove fragment identifier (#) if present
                    var filePath = navItem.Link.ContentFilePath.Split('#')[0];
                    
                    // Use the first title found for each file path
                    if (!lookup.ContainsKey(filePath))
                    {
                        lookup[filePath] = navItem.Title.Trim();
                        _logger.LogDebug("Mapped navigation: {FilePath} -> '{Title}'", filePath, navItem.Title);
                    }
                }
                
                // Process nested navigation items recursively
                if (navItem.NestedItems?.Any() == true)
                {
                    _logger.LogDebug("Processing {Count} nested items for '{Title}'", navItem.NestedItems.Count, navItem.Title);
                    ProcessNavigationItems(navItem.NestedItems);
                }
            }
        }
        
        ProcessNavigationItems(navigationItems);
        _logger.LogInformation("Created navigation lookup with {Count} entries", lookup.Count);
        return lookup;
    }

    /// <summary>
    /// Gets the real title from navigation lookup, or returns null if not found
    /// </summary>
    private string? GetRealTitle(string filePath, Dictionary<string, string> navigationLookup)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        // Try exact match first
        if (navigationLookup.TryGetValue(filePath, out var title))
        {
            return title;
        }

        // Try without the OEBPS/ prefix if present
        var pathWithoutPrefix = filePath.StartsWith("OEBPS/", StringComparison.OrdinalIgnoreCase) 
            ? filePath.Substring(6) 
            : filePath;
            
        if (navigationLookup.TryGetValue(pathWithoutPrefix, out title))
        {
            return title;
        }

        // Try with OEBPS/ prefix if not present
        var pathWithPrefix = filePath.StartsWith("OEBPS/", StringComparison.OrdinalIgnoreCase) 
            ? filePath 
            : $"OEBPS/{filePath}";
            
        if (navigationLookup.TryGetValue(pathWithPrefix, out title))
        {
            return title;
        }

        // Log when we can't find a title
        _logger.LogDebug("Could not find navigation title for file path: {FilePath}", filePath);
        return null;
    }

    /// <summary>
    /// Attempts to extract title from HTML content file
    /// </summary>
    private string? TryExtractTitleFromHtml(EpubBook epubBook, string filePath)
    {
        try
        {
            // Find the HTML content file in the EPUB
            var htmlFile = epubBook.ReadingOrder.FirstOrDefault(f => f.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
            if (htmlFile == null)
                return null;

            var content = htmlFile.Content;
            if (string.IsNullOrWhiteSpace(content))
                return null;

            // Try to extract title from <title> tag
            var titleMatch = System.Text.RegularExpressions.Regex.Match(
                content, 
                @"<title[^>]*>([^<]+)</title>", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
            if (titleMatch.Success)
            {
                var titleText = titleMatch.Groups[1].Value.Trim();
                _logger.LogDebug("Extracted title from HTML: '{Title}' from file {FilePath}", titleText, filePath);
                return titleText;
            }

            // Try to extract from h2 with class="title" (from the sample)
            var h2Match = System.Text.RegularExpressions.Regex.Match(
                content, 
                @"<h2[^>]*class=""title""[^>]*>([^<]+)</h2>", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
            if (h2Match.Success)
            {
                var h2Text = h2Match.Groups[1].Value.Trim();
                _logger.LogDebug("Extracted title from H2: '{Title}' from file {FilePath}", h2Text, filePath);
                return h2Text;
            }

            // Try to extract from any h1, h2 etc.
            var headerMatch = System.Text.RegularExpressions.Regex.Match(
                content, 
                @"<h[1-6][^>]*>([^<]+)</h[1-6]>", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
            if (headerMatch.Success)
            {
                var headerText = headerMatch.Groups[1].Value.Trim();
                _logger.LogDebug("Extracted title from header: '{Title}' from file {FilePath}", headerText, filePath);
                return headerText;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract title from HTML file: {FilePath}", filePath);
            return null;
        }
    }

    public async Task<EpubMetadataDto> ExtractMetadataAsync(string epubFilePath)
    {
        try
        {
            var epubBook = await EpubReader.ReadBookAsync(epubFilePath);
            
            // Extract metadata from Schema.Package instead of direct properties
            var metadata = epubBook.Schema.Package.Metadata;
            
            // Extract publication date
            DateTime? publishedDate = null;
            var dateElements = metadata.Dates;
            if (dateElements?.Any() == true)
            {
                // Look for publication date first, then any date
                var publicationDate = dateElements.FirstOrDefault(d => 
                    d.Event?.Equals("publication", StringComparison.OrdinalIgnoreCase) == true);
                
                var dateToUse = publicationDate ?? dateElements.First();
                
                if (DateTime.TryParse(dateToUse.Date, out var parsedDate))
                {
                    publishedDate = parsedDate;
                }
            }
            
            var result = new EpubMetadataDto
            {
                Title = epubBook.Title ?? string.Empty,
                Author = string.Join(", ", epubBook.AuthorList ?? new List<string>()),
                Publisher = metadata.Publishers?.FirstOrDefault()?.Publisher ?? string.Empty,
                Language = metadata.Languages?.FirstOrDefault()?.Language ?? "vi",
                Description = metadata.Descriptions?.FirstOrDefault()?.Description ?? string.Empty,
                CoverImageBytes = epubBook.CoverImage,
                TotalPages = epubBook.ReadingOrder?.Count ?? 0,
                PublishedDate = publishedDate
            };

            _logger.LogInformation("Successfully extracted metadata for EPUB: Title={Title}, Author={Author}, Publisher={Publisher}", 
                result.Title, result.Author, result.Publisher);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract metadata from EPUB file: {FilePath}", epubFilePath);
            throw;
        }
    }

    public string ProcessEpubFileWithContent(Guid bookId, string userId, byte[] fileContent, string fileExtension)
    {
        try
        {
            var jobId = _fileBackgroundService.QueueEpubProcessingWithContent(bookId, userId, fileContent, fileExtension);
            _logger.LogInformation("Queued EPUB processing with content for book {BookId} with job {JobId}", bookId, jobId);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue EPUB processing with content for book {BookId}", bookId);
            throw;
        }
    }
} 