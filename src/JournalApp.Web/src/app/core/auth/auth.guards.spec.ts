import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Route, Router, UrlSegment, UrlTree, provideRouter } from '@angular/router';
import { authGuard, guestGuard } from './auth.guards';
import { AuthStore } from './auth.store';

describe('auth guards', () => {
  const isAuthenticated = signal(false);

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthStore, useValue: { isAuthenticated } }],
    });
  });

  const run = (guard: typeof authGuard, path: string[] = []) =>
    TestBed.runInInjectionContext(() =>
      guard(
        {} as Route,
        path.map((p) => new UrlSegment(p, {})),
      ),
    );

  it('authGuard redirects anonymous users to /login with the return url', () => {
    isAuthenticated.set(false);

    const result = run(authGuard, ['entries', '42']) as UrlTree;

    expect(TestBed.inject(Router).serializeUrl(result)).toBe('/login?returnUrl=%2Fentries%2F42');
  });

  it('authGuard lets authenticated users in', () => {
    isAuthenticated.set(true);

    expect(run(authGuard)).toBe(true);
  });

  it('guestGuard sends authenticated users to /entries', () => {
    isAuthenticated.set(true);

    const result = run(guestGuard) as UrlTree;

    expect(TestBed.inject(Router).serializeUrl(result)).toBe('/entries');
  });

  it('guestGuard lets anonymous users in', () => {
    isAuthenticated.set(false);

    expect(run(guestGuard)).toBe(true);
  });
});
