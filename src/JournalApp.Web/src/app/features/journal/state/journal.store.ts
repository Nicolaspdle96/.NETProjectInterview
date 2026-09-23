import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthStore } from '../../../core/auth/auth.store';
import { AppError } from '../../../domain/models/app-error';
import { JournalEntry, JournalEntryDraft } from '../../../domain/models/journal-entry';
import { JournalEntryRepository } from '../../../domain/repositories/journal-entry.repository';

interface JournalState {
  ownerId: string | null;
  entries: JournalEntry[];
  loaded: boolean;
  loading: boolean;
  error: AppError | null;
}

const initialState = (ownerId: string | null): JournalState => ({
  ownerId,
  entries: [],
  loaded: false,
  loading: false,
  error: null,
});

const byNewestFirst = (a: JournalEntry, b: JournalEntry) =>
  b.createdAt.getTime() - a.createdAt.getTime();

/** State of the current user's journal entries. */
@Injectable({ providedIn: 'root' })
export class JournalStore {
  private readonly repository = inject(JournalEntryRepository);
  private readonly authStore = inject(AuthStore);

  private readonly state = signal<JournalState>(initialState(null));

  readonly entries = computed(() => [...this.state().entries].sort(byNewestFirst));
  readonly loading = computed(() => this.state().loading);
  readonly loaded = computed(() => this.state().loaded);
  readonly error = computed(() => this.state().error);
  readonly isEmpty = computed(() => this.state().loaded && this.state().entries.length === 0);

  async load(): Promise<void> {
    this.ensureCurrentOwner();
    this.patch({ loading: true, error: null });
    try {
      const entries = await firstValueFrom(this.repository.list());
      this.patch({ entries, loaded: true });
    } catch (error) {
      this.patch({ error: AppError.from(error) });
    } finally {
      this.patch({ loading: false });
    }
  }

  /** Entry already in memory (e.g. coming from the list), used to render instantly. */
  cached(id: string): JournalEntry | undefined {
    this.ensureCurrentOwner();
    return this.state().entries.find((entry) => entry.id === id);
  }

  async getById(id: string): Promise<JournalEntry> {
    this.ensureCurrentOwner();
    const entry = await firstValueFrom(this.repository.getById(id));
    this.upsert(entry);
    return entry;
  }

  async create(draft: JournalEntryDraft): Promise<JournalEntry> {
    this.ensureCurrentOwner();
    const entry = await firstValueFrom(this.repository.create(draft));
    this.upsert(entry);
    return entry;
  }

  async update(id: string, draft: JournalEntryDraft): Promise<JournalEntry> {
    this.ensureCurrentOwner();
    const entry = await firstValueFrom(this.repository.update(id, draft));
    this.upsert(entry);
    return entry;
  }

  async remove(id: string): Promise<void> {
    this.ensureCurrentOwner();
    await firstValueFrom(this.repository.delete(id), { defaultValue: undefined });
    this.patch({ entries: this.state().entries.filter((entry) => entry.id !== id) });
  }

  private upsert(entry: JournalEntry): void {
    const others = this.state().entries.filter((existing) => existing.id !== entry.id);
    this.patch({ entries: [...others, entry] });
  }

  /** Drops cached data when a different user signs in. */
  private ensureCurrentOwner(): void {
    const ownerId = this.authStore.user()?.id ?? null;
    if (ownerId !== this.state().ownerId) {
      this.state.set(initialState(ownerId));
    }
  }

  private patch(changes: Partial<JournalState>): void {
    this.state.update((current) => ({ ...current, ...changes }));
  }
}
