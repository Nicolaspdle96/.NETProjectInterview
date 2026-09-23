import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthStore } from './auth.store';

/** Lets only authenticated users in; others go to /login and come back afterwards. */
export const authGuard: CanMatchFn = (_route, segments) => {
  if (inject(AuthStore).isAuthenticated()) {
    return true;
  }
  const returnUrl = '/' + segments.map((s) => s.path).join('/');
  return inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl } });
};

/**
 * Keeps signed-in users away from the login and register pages. It is attached to a
 * `path: ''` route, so it runs for every URL: it must return `false` (not a redirect) so
 * other routes can match; the `**` route sends signed-in users to /entries.
 */
export const guestGuard: CanMatchFn = () => !inject(AuthStore).isAuthenticated();
