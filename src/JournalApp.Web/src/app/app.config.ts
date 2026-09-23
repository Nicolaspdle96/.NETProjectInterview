import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';

import { routes } from './app.routes';
import { authInterceptor } from './core/http/auth.interceptor';
import { errorInterceptor } from './core/http/error.interceptor';
import { AuthHttpRepository } from './data/repositories/auth-http.repository';
import { JournalEntryHttpRepository } from './data/repositories/journal-entry-http.repository';
import { AuthRepository } from './domain/repositories/auth.repository';
import { JournalEntryRepository } from './domain/repositories/journal-entry.repository';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),

    // Domain contracts → HTTP implementations.
    { provide: AuthRepository, useClass: AuthHttpRepository },
    { provide: JournalEntryRepository, useClass: JournalEntryHttpRepository },
  ],
};
