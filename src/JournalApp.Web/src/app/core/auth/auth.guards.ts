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

/** Keeps signed-in users away from the login and register pages. */
export const guestGuard: CanMatchFn = () =>
  inject(AuthStore).isAuthenticated() ? inject(Router).createUrlTree(['/entries']) : true;
