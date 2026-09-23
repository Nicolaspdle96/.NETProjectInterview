import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthStore } from './core/auth/auth.store';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink],
  template: `
    <a class="skip-link" href="#main-content">Skip to content</a>
    <header class="app-header">
      <div class="container app-header__inner">
        <a class="app-header__brand" [routerLink]="auth.isAuthenticated() ? '/entries' : '/login'">
          <span aria-hidden="true">📔</span> Journal
        </a>
        @if (auth.user(); as user) {
          <nav class="app-header__nav" aria-label="Account">
            <span class="app-header__user" title="{{ user.email }}">{{ user.username }}</span>
            <button type="button" class="btn btn--ghost btn--small" (click)="auth.logout()">
              Sign out
            </button>
          </nav>
        }
      </div>
    </header>
    <main id="main-content" class="container app-main" tabindex="-1">
      <router-outlet />
    </main>
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      min-height: 100vh;
    }
    .skip-link {
      position: absolute;
      left: -999px;
      top: 0.5rem;
      z-index: 10;
      padding: 0.5rem 1rem;
      background: var(--color-surface);
      border-radius: var(--radius);
    }
    .skip-link:focus {
      left: 0.5rem;
    }
    .app-header {
      position: sticky;
      top: 0;
      z-index: 5;
      background: color-mix(in srgb, var(--color-bg) 88%, transparent);
      backdrop-filter: blur(8px);
      border-bottom: 1px solid var(--color-border);
    }
    .app-header__inner {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      min-height: 3.75rem;
    }
    .app-header__brand {
      font-family: var(--font-serif);
      font-size: 1.35rem;
      font-weight: 700;
      color: var(--color-text);
      text-decoration: none;
    }
    .app-header__nav {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      min-width: 0;
    }
    .app-header__user {
      overflow: hidden;
      max-width: 10rem;
      font-weight: 600;
      text-overflow: ellipsis;
      white-space: nowrap;
      color: var(--color-text-muted);
    }
    .app-main {
      flex: 1;
      width: 100%;
      padding-top: 1.5rem;
      padding-bottom: 3rem;
      outline: none;
    }
  `,
})
export class App {
  protected readonly auth = inject(AuthStore);
}
