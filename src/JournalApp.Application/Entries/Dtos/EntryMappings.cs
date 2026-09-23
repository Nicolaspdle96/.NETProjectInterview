using JournalApp.Domain.Entities;

namespace JournalApp.Application.Entries.Dtos;

public static class EntryMappings
{
    public static EntryDto ToDto(this JournalEntry entry) =>
        new(entry.Id, entry.Title, entry.Content, entry.Mood, entry.CreatedAt, entry.UpdatedAt);
}
