import { InjectionToken } from '@angular/core';

/**
 * Relative on purpose: the dev server proxies /api to the backend, and in production
 * the same .NET process serves both the API and this app.
 */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => '/api',
});
