using JournalApp.Application.Common;
using JournalApp.Application.Common.Caching;
using JournalApp.Application.Common.Interfaces;
using JournalApp.Application.Entries.Dtos;

namespace JournalApp.Application.Entries;

/// <summary>
/// Caching decorator over <see cref="JournalEntryService"/>. Reads are served from the cache
/// when possible; any write by a user invalidates all of that user's cached reads.
/// Business rules (validation, ownership) stay in the inner service.
/// </summary>
public class CachedJournalEntryService(
    IJournalEntryService inner,
    ICacheService cache,
    ICurrentUserService currentUser) : IJournalEntryService
{
    public async Task<IReadOnlyList<EntryDto>> ListAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();
        var version = await GetVersionAsync(userId, cancellationToken);

        return await cache.GetOrCreateAsync(
            CacheKeys.EntryList(userId, version),
            async ct => (IReadOnlyList<EntryDto>)(await inner.ListAsync(ct)).ToList().AsReadOnly(),
            cancellationToken);
    }

    public async Task<EntryDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();
        var version = await GetVersionAsync(userId, cancellationToken);

        return await cache.GetOrCreateAsync(
            CacheKeys.Entry(userId, version, id),
            ct => inner.GetByIdAsync(id, ct),
            cancellationToken);
    }

    public async Task<EntryDto> CreateAsync(CreateEntryRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();
        try
        {
            return await inner.CreateAsync(request, cancellationToken);
        }
        finally
        {
            await InvalidateAsync(userId);
        }
    }

    public async Task<EntryDto> UpdateAsync(Guid id, UpdateEntryRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();
        try
        {
            return await inner.UpdateAsync(id, request, cancellationToken);
        }
        finally
        {
            await InvalidateAsync(userId);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();
        try
        {
            await inner.DeleteAsync(id, cancellationToken);
        }
        finally
        {
            await InvalidateAsync(userId);
        }
    }

    private Task<string> GetVersionAsync(Guid userId, CancellationToken cancellationToken) =>
        cache.GetOrCreateAsync(
            CacheKeys.EntriesVersion(userId),
            _ => Task.FromResult(Guid.NewGuid().ToString("N")),
            cancellationToken);

    // Runs even if the write failed or the request was cancelled after the database changed,
    // so a cached read can never outlive the data it came from.
    private Task InvalidateAsync(Guid userId) =>
        cache.RemoveAsync(CacheKeys.EntriesVersion(userId), CancellationToken.None);
}
