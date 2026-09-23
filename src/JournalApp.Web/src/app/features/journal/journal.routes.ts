import { Routes } from '@angular/router';

export const JOURNAL_ROUTES: Routes = [
  {
    path: '',
    title: 'My journal',
    loadComponent: () =>
      import('./pages/entry-list-page.component').then((m) => m.EntryListPageComponent),
  },
  {
    path: 'new',
    title: 'New entry · Journal',
    loadComponent: () =>
      import('./pages/entry-form-page.component').then((m) => m.EntryFormPageComponent),
  },
  {
    path: ':id',
    title: 'Entry · Journal',
    loadComponent: () =>
      import('./pages/entry-detail-page.component').then((m) => m.EntryDetailPageComponent),
  },
  {
    path: ':id/edit',
    title: 'Edit entry · Journal',
    loadComponent: () =>
      import('./pages/entry-form-page.component').then((m) => m.EntryFormPageComponent),
  },
];
