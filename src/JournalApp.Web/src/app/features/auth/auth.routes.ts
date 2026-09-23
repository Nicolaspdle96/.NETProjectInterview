import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  {
    path: 'login',
    title: 'Sign in · Journal',
    loadComponent: () => import('./pages/login-page.component').then((m) => m.LoginPageComponent),
  },
  {
    path: 'register',
    title: 'Create account · Journal',
    loadComponent: () =>
      import('./pages/register-page.component').then((m) => m.RegisterPageComponent),
  },
];
