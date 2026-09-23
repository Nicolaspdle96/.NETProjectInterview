import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guards';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'entries' },
  {
    path: '',
    canMatch: [guestGuard],
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },
  {
    path: 'entries',
    canMatch: [authGuard],
    loadChildren: () => import('./features/journal/journal.routes').then((m) => m.JOURNAL_ROUTES),
  },
  { path: '**', redirectTo: 'entries' },
];
