import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../../core/config/api.config';
import { JournalEntry, JournalEntryDraft } from '../../domain/models/journal-entry';
import { JournalEntryRepository } from '../../domain/repositories/journal-entry.repository';
import { EntryDto } from '../dtos/entry.dto';
import { toJournalEntry, toSaveEntryRequest } from '../mappers/entry.mapper';
import { mapHttpErrors } from '../mappers/error.mapper';

@Injectable()
export class JournalEntryHttpRepository extends JournalEntryRepository {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/entries`;

  list(): Observable<JournalEntry[]> {
    return this.http.get<EntryDto[]>(this.baseUrl).pipe(
      map((dtos) => dtos.map(toJournalEntry)),
      mapHttpErrors(),
    );
  }

  getById(id: string): Observable<JournalEntry> {
    return this.http
      .get<EntryDto>(`${this.baseUrl}/${encodeURIComponent(id)}`)
      .pipe(map(toJournalEntry), mapHttpErrors());
  }

  create(draft: JournalEntryDraft): Observable<JournalEntry> {
    return this.http
      .post<EntryDto>(this.baseUrl, toSaveEntryRequest(draft))
      .pipe(map(toJournalEntry), mapHttpErrors());
  }

  update(id: string, draft: JournalEntryDraft): Observable<JournalEntry> {
    return this.http
      .put<EntryDto>(`${this.baseUrl}/${encodeURIComponent(id)}`, toSaveEntryRequest(draft))
      .pipe(map(toJournalEntry), mapHttpErrors());
  }

  delete(id: string): Observable<void> {
    return this.http
      .delete<void>(`${this.baseUrl}/${encodeURIComponent(id)}`)
      .pipe(mapHttpErrors());
  }
}
