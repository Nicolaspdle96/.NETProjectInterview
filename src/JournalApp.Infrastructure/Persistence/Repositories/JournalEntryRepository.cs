using JournalApp.Application.Common.Interfaces;
using JournalApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JournalApp.Infrastructure.Persistence.Repositories;

public class JournalEntryRepository(AppDbContext context) : IJournalEntryRepository
{
    public Task<JournalEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.JournalEntries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<JournalEntry>> ListByUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await context.JournalEntries
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(JournalEntry entry, CancellationToken cancellationToken) =>
        await context.JournalEntries.AddAsync(entry, cancellationToken);

    public void Remove(JournalEntry entry) => context.JournalEntries.Remove(entry);
}
