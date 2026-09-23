using JournalApp.Domain.Entities;

namespace JournalApp.Application.Common.Interfaces;

public interface IJournalEntryRepository
{
    Task<JournalEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Returns the user's entries sorted by creation date, newest first.</summary>
    Task<IReadOnlyList<JournalEntry>> ListByUserAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(JournalEntry entry, CancellationToken cancellationToken);

    void Remove(JournalEntry entry);
}
