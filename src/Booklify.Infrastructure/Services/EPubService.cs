using Microsoft.Extensions.Logging;
using Booklify.Application.Common.Interfaces;
using Booklify.Domain.Commons;
using Booklify.Domain.Entities;
using Booklify.Domain.Enums;
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

    public async Task<List<Chapter>> ExtractChapters(string epubFilePath)
    {
        var epubBook = await EpubReader.ReadBookAsync(epubFilePath);
        var chapters = new List<Chapter>();
        await TraverseNavigationItems(epubBook.Navigation, chapters, null, 1);
        return chapters;
    }

    private async Task TraverseNavigationItems(
        ICollection<EpubNavigationItem> navigationItems,
        List<Chapter> chapters,
        Guid? parentChapterId,
        int orderStart)
    {
        int order = orderStart;

        foreach (var navigationItem in navigationItems)
        {
            string href = navigationItem.Link?.ContentFilePath ?? 
                         navigationItem.HtmlContentFile?.FilePath ?? 
                         string.Empty;
            
            // Tạm thời bỏ CFI generation để focus vào core functionality
            string? cfi = null;
            
            // Nếu muốn CFI đơn giản từ anchor
            if (navigationItem.Link?.Anchor != null)
            {
                cfi = $"#{navigationItem.Link.Anchor}";
            }

            var chapter = new Chapter
            {
                Title = navigationItem.Title,
                Order = order++,
                Href = href,
                Cfi = cfi,
                ParentChapterId = parentChapterId,
            };
            
            BaseEntityExtensions.InitializeBaseEntity(chapter, (Guid?)null);
            chapters.Add(chapter);

            if (navigationItem.NestedItems?.Any() == true)
            {
                await TraverseNavigationItems(
                    navigationItem.NestedItems,
                    chapters,
                    chapter.Id,
                    1);
            }
        }
    }

    public string ProcessEpubFile(Guid bookId, string userId = "", FileUploadType uploadType = FileUploadType.None)
    {
        try
        {
            var jobId = _fileBackgroundService.QueueEpubProcessing(bookId, userId);
            _logger.LogInformation("Queued EPUB processing for book {BookId} with job {JobId}", bookId, jobId);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue EPUB processing for book {BookId}", bookId);
            throw;
        }
    }
} 