import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { AppError } from '../../domain/models/app-error';
import { JournalEntryHttpRepository } from './journal-entry-http.repository';

describe('JournalEntryHttpRepository', () => {
  let repository: JournalEntryHttpRepository;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), JournalEntryHttpRepository],
    });
    repository = TestBed.inject(JournalEntryHttpRepository);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists entries from the relative /api base url and maps them', async () => {
    const result = firstValueFrom(repository.list());

    http
      .expectOne({ method: 'GET', url: '/api/entries' })
      .flush([
        {
          id: '1',
          title: 'A',
          content: 'B',
          mood: 'Good',
          createdAt: '2026-01-01T00:00:00Z',
          updatedAt: null,
        },
      ]);

    const entries = await result;
    expect(entries).toHaveLength(1);
    expect(entries[0].createdAt).toBeInstanceOf(Date);
  });

  it('sends the draft with a trimmed title when creating', async () => {
    const result = firstValueFrom(
      repository.create({ title: '  New ', content: 'Body', mood: 'Bad' }),
    );

    const request = http.expectOne({ method: 'POST', url: '/api/entries' });
    expect(request.request.body).toEqual({ title: 'New', content: 'Body', mood: 'Bad' });
    request.flush({
      id: '9',
      title: 'New',
      content: 'Body',
      mood: 'Bad',
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: null,
    });

    expect((await result).id).toBe('9');
  });

  it('converts HTTP errors into AppError', async () => {
    const result = firstValueFrom(repository.getById('missing'));

    http
      .expectOne('/api/entries/missing')
      .flush({ title: 'Resource not found.' }, { status: 404, statusText: 'Not Found' });

    await expect(result).rejects.toSatisfy(
      (error: unknown) => error instanceof AppError && error.kind === 'notFound',
    );
  });
});
