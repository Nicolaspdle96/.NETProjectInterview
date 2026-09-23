using JournalApp.Domain.Enums;

namespace JournalApp.Application.Entries.Dtos;

public record CreateEntryRequest(string Title, string Content, Mood? Mood);

public record UpdateEntryRequest(string Title, string Content, Mood? Mood);

public record EntryDto(Guid Id, string Title, string Content, Mood? Mood, DateTime CreatedAt, DateTime? UpdatedAt);
