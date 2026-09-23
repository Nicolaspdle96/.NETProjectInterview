import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { AuthStore } from './core/auth/auth.store';
import { JournalEntryRepository } from './domain/repositories/journal-entry.repository';
import { AuthRepository } from './domain/repositories/auth.repository';
import { NEVER } from 'rxjs';

describe('app routes', () => {
  const isAuthenticated = signal(false);

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        {
          provide: AuthStore,
          useValue: { isAuthenticated, user: signal(null), token: signal(null) },
        },
        { provide: AuthRepository, useValue: {} },
        { provide: JournalEntryRepository, useValue: { list: () => NEVER } },
      ],
    });
  });

  it.each([
    [false, '/entries', '/login?returnUrl=%2Fentries'],
    [false, '/register', '/register'],
    [true, '/entries', '/entries'],
    [true, '/login', '/entries'],
    [true, '/unknown', '/entries'],
  ])('authenticated=%s: %s resolves to %s', async (authenticated, url, expected) => {
    isAuthenticated.set(authenticated);
    const router = TestBed.inject(Router);

    await router.navigateByUrl(url);

    expect(router.url).toBe(expected);
  });
});
