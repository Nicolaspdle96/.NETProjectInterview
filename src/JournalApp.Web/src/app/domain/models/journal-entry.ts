import { Mood } from './mood';

export interface JournalEntry {
  readonly id: string;
  readonly title: string;
  readonly content: string;
  readonly mood: Mood | null;
  readonly createdAt: Date;
  readonly updatedAt: Date | null;
}

/** Data the user provides when creating or editing an entry. */
export interface JournalEntryDraft {
  readonly title: string;
  readonly content: string;
  readonly mood: Mood | null;
}

/** Business rules shared with the backend. */
export const ENTRY_RULES = {
  titleMaxLength: 100,
  contentMaxLength: 5000,
} as const;
