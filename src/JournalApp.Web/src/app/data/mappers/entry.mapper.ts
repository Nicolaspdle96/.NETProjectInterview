import { EntryDto, SaveEntryRequestDto } from '../dtos/entry.dto';
import { JournalEntry, JournalEntryDraft } from '../../domain/models/journal-entry';
import { isMood } from '../../domain/models/mood';

export function toJournalEntry(dto: EntryDto): JournalEntry {
  return {
    id: dto.id,
    title: dto.title,
    content: dto.content,
    mood: isMood(dto.mood) ? dto.mood : null,
    createdAt: new Date(dto.createdAt),
    updatedAt: dto.updatedAt ? new Date(dto.updatedAt) : null,
  };
}

export function toSaveEntryRequest(draft: JournalEntryDraft): SaveEntryRequestDto {
  return {
    title: draft.title.trim(),
    content: draft.content,
    mood: draft.mood,
  };
}
