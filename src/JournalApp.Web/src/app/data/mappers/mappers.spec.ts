import { HttpErrorResponse } from '@angular/common/http';
import { toJournalEntry, toSaveEntryRequest } from './entry.mapper';
import { toAppError } from './error.mapper';
import { toAuthSession } from './user.mapper';

describe('entry mapper', () => {
  it('maps date strings to Date objects and keeps a valid mood', () => {
    const entry = toJournalEntry({
      id: '1',
      title: 'Title',
      content: 'Content',
      mood: 'Great',
      createdAt: '2026-01-15T10:30:00Z',
      updatedAt: null,
    });

    expect(entry.createdAt).toBeInstanceOf(Date);
    expect(entry.createdAt.toISOString()).toBe('2026-01-15T10:30:00.000Z');
    expect(entry.updatedAt).toBeNull();
    expect(entry.mood).toBe('Great');
  });

  it('maps an unknown mood to null', () => {
    const entry = toJournalEntry({
      id: '1',
      title: 'Title',
      content: 'Content',
      mood: 'Ecstatic',
      createdAt: '2026-01-15T10:30:00Z',
      updatedAt: '2026-01-16T10:30:00Z',
    });

    expect(entry.mood).toBeNull();
    expect(entry.updatedAt?.toISOString()).toBe('2026-01-16T10:30:00.000Z');
  });

  it('trims the title when building the request', () => {
    const request = toSaveEntryRequest({ title: '  Hello  ', content: ' Body ', mood: null });

    expect(request).toEqual({ title: 'Hello', content: ' Body ', mood: null });
  });
});

describe('user mapper', () => {
  it('maps the auth response to a session', () => {
    const session = toAuthSession({
      token: 'jwt',
      expiresAt: '2026-01-15T11:00:00Z',
      user: {
        id: 'u1',
        username: 'demo',
        email: 'demo@journal.com',
        createdAt: '2026-01-01T00:00:00Z',
      },
    });

    expect(session.token).toBe('jwt');
    expect(session.expiresAt).toBeInstanceOf(Date);
    expect(session.user.createdAt).toBeInstanceOf(Date);
  });
});

describe('error mapper', () => {
  it('maps a 400 ProblemDetails to a validation error with camelCase field errors', () => {
    const error = toAppError(
      new HttpErrorResponse({
        status: 400,
        error: {
          title: 'Validation',
          errors: { Title: ['Title is required.'], '$.mood': ['Bad value'] },
        },
      }),
    );

    expect(error.kind).toBe('validation');
    expect(error.fieldErrors).toEqual({ title: ['Title is required.'], mood: ['Bad value'] });
  });

  it.each([
    [0, 'network'],
    [401, 'unauthorized'],
    [404, 'notFound'],
    [409, 'conflict'],
    [500, 'unknown'],
  ] as const)('maps status %i to %s', (status, kind) => {
    expect(toAppError(new HttpErrorResponse({ status })).kind).toBe(kind);
  });

  it('uses the server detail message for conflicts', () => {
    const error = toAppError(
      new HttpErrorResponse({
        status: 409,
        error: { title: 'Conflict.', detail: 'The email is already in use.' },
      }),
    );

    expect(error.message).toBe('The email is already in use.');
  });
});
