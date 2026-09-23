import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AuthStore } from '../../../core/auth/auth.store';
import { AppError } from '../../../domain/models/app-error';
import { JournalEntry } from '../../../domain/models/journal-entry';
import { JournalEntryRepository } from '../../../domain/repositories/journal-entry.repository';
import { JournalStore } from './journal.store';

const entry = (id: string, createdAt: string, title = `Entry ${id}`): JournalEntry => ({
  id,
  title,
  content: 'Content',
  mood: 'Good',
  createdAt: new Date(createdAt),
  updatedAt: null,
});

describe('JournalStore', () => {
  const user = signal<{ id: string } | null>({ id: 'u1' });
  let repository: Record<
    'list' | 'getById' | 'create' | 'update' | 'delete',
    ReturnType<typeof vi.fn>
  >;
  let store: JournalStore;

  beforeEach(() => {
    user.set({ id: 'u1' });
    repository = {
      list: vi.fn(),
      getById: vi.fn(),
      create: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
    };
    TestBed.configureTestingModule({
      providers: [
        { provide: JournalEntryRepository, useValue: repository },
        { provide: AuthStore, useValue: { user } },
      ],
    });
    store = TestBed.inject(JournalStore);
  });

  it('loads entries and exposes them newest first', async () => {
    repository.list.mockReturnValue(
      of([entry('old', '2026-01-01T00:00:00Z'), entry('new', '2026-03-01T00:00:00Z')]),
    );

    await store.load();

    expect(store.entries().map((e) => e.id)).toEqual(['new', 'old']);
    expect(store.loaded()).toBe(true);
    expect(store.loading()).toBe(false);
    expect(store.isEmpty()).toBe(false);
  });

  it('flags the empty state when the user has no entries', async () => {
    repository.list.mockReturnValue(of([]));

    await store.load();

    expect(store.isEmpty()).toBe(true);
  });

  it('exposes the error when loading fails', async () => {
    repository.list.mockReturnValue(throwError(() => new AppError('network', 'Offline')));

    await store.load();

    expect(store.error()?.message).toBe('Offline');
    expect(store.loading()).toBe(false);
  });

  it('adds created entries to the list in date order', async () => {
    repository.list.mockReturnValue(of([entry('a', '2026-01-01T00:00:00Z')]));
    await store.load();
    repository.create.mockReturnValue(of(entry('b', '2026-02-01T00:00:00Z')));

    await store.create({ title: 'B', content: 'Content', mood: null });

    expect(store.entries().map((e) => e.id)).toEqual(['b', 'a']);
  });

  it('replaces updated entries', async () => {
    repository.list.mockReturnValue(of([entry('a', '2026-01-01T00:00:00Z', 'Before')]));
    await store.load();
    repository.update.mockReturnValue(of(entry('a', '2026-01-01T00:00:00Z', 'After')));

    await store.update('a', { title: 'After', content: 'Content', mood: null });

    expect(store.entries()).toHaveLength(1);
    expect(store.entries()[0].title).toBe('After');
  });

  it('removes deleted entries', async () => {
    repository.list.mockReturnValue(of([entry('a', '2026-01-01T00:00:00Z')]));
    await store.load();
    repository.delete.mockReturnValue(of(undefined));

    await store.remove('a');

    expect(store.entries()).toEqual([]);
  });

  it('drops cached entries when a different user signs in', async () => {
    repository.list.mockReturnValue(of([entry('a', '2026-01-01T00:00:00Z')]));
    await store.load();

    user.set({ id: 'u2' });

    expect(store.cached('a')).toBeUndefined();
  });
});
