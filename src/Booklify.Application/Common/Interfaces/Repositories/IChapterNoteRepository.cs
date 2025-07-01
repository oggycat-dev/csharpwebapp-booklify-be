using Booklify.Application.Common.DTOs.ChapterNote;
using Booklify.Domain.Entities;

namespace Booklify.Application.Common.Interfaces.Repositories;

public interface IChapterNoteRepository : IGenericRepository<ChapterNote>
{
    Task<(List<ChapterNote> Notes, int TotalCount)> GetPagedChapterNotesAsync(ChapterNoteFilterModel filter);
} 