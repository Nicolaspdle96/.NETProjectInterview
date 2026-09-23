import { HttpErrorResponse, HttpInterceptorFn, HttpStatusCode } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthStore } from '../auth/auth.store';

/** A 401 on an authenticated request means the session is gone: sign out and go to /login. */
export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const authStore = inject(AuthStore);
  return next(request).pipe(
    catchError((error: unknown) => {
      const isUnauthorized =
        error instanceof HttpErrorResponse && error.status === HttpStatusCode.Unauthorized;
      if (isUnauthorized && request.headers.has('Authorization')) {
        authStore.logout();
      }
      return throwError(() => error);
    }),
  );
};
