import { Observable } from 'rxjs';
import { JournalEntry, JournalEntryDraft } from '../models/journal-entry';

/** Contract used as a DI token; bound to its HTTP implementation in app.config.ts. */
export abstract class JournalEntryRepository {
  /** Entries of the current user, newest first. */
  abstract list(): Observable<JournalEntry[]>;
  abstract getById(id: string): Observable<JournalEntry>;
  abstract create(draft: JournalEntryDraft): Observable<JournalEntry>;
  abstract update(id: string, draft: JournalEntryDraft): Observable<JournalEntry>;
  abstract delete(id: string): Observable<void>;
}
