using FluentValidation;
using JournalApp.Application.Common;
using JournalApp.Application.Common.Exceptions;
using JournalApp.Application.Common.Interfaces;
using JournalApp.Application.Entries.Dtos;
using JournalApp.Domain.Entities;
using JournalApp.Domain.Exceptions;

namespace JournalApp.Application.Entries;

public interface IJournalEntryService
{
    Task<IReadOnlyList<EntryDto>> ListAsync(CancellationToken cancellationToken);

    Task<EntryDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<EntryDto> CreateAsync(CreateEntryRequest request, CancellationToken cancellationToken);

    Task<EntryDto> UpdateAsync(Guid id, UpdateEntryRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public class JournalEntryService(
    IJournalEntryRepository entries,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork,
    IValidator<CreateEntryRequest> createValidator,
    IValidator<UpdateEntryRequest> updateValidator,
    TimeProvider timeProvider) : IJournalEntryService
{
    public async Task<IReadOnlyList<EntryDto>> ListAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();
        var userEntries = await entries.ListByUserAsync(userId, cancellationToken);
        return userEntries.Select(e => e.ToDto()).ToList();
    }

    public async Task<EntryDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await GetOwnedEntryAsync(id, cancellationToken);
        return entry.ToDto();
    }

    public async Task<EntryDto> CreateAsync(CreateEntryRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();
        await createValidator.EnsureValidAsync(request, cancellationToken);

        var entry = JournalEntry.Create(
            userId, request.Title, request.Content, request.Mood, timeProvider.GetUtcNow().UtcDateTime);

        await entries.AddAsync(entry, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return entry.ToDto();
    }

    public async Task<EntryDto> UpdateAsync(Guid id, UpdateEntryRequest request, CancellationToken cancellationToken)
    {
        var entry = await GetOwnedEntryAsync(id, cancellationToken);
        await updateValidator.EnsureValidAsync(request, cancellationToken);

        entry.Update(request.Title, request.Content, request.Mood, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return entry.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await GetOwnedEntryAsync(id, cancellationToken);

        entries.Remove(entry);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Loads an entry that belongs to the current user. Entries owned by someone else are
    /// reported as not found so their existence is not revealed.
    /// </summary>
    private async Task<JournalEntry> GetOwnedEntryAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();
        var entry = await entries.GetByIdAsync(id, cancellationToken);

        if (entry is null || !entry.IsOwnedBy(userId))
        {
            throw new NotFoundException($"Journal entry '{id}' was not found.");
        }

        return entry;
    }
}
