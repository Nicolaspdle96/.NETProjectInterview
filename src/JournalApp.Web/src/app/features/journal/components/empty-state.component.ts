import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="empty-state card">
      <span class="empty-state__icon" aria-hidden="true">{{ icon() }}</span>
      <h2 class="empty-state__title">{{ title() }}</h2>
      <p class="empty-state__message">{{ message() }}</p>
      <ng-content />
    </div>
  `,
  styles: `
    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.75rem;
      padding: 2.5rem 1.5rem;
      text-align: center;
    }
    .empty-state__icon {
      font-size: 2.75rem;
    }
    .empty-state__title {
      margin: 0;
      font-family: var(--font-serif);
      font-size: 1.4rem;
    }
    .empty-state__message {
      margin: 0 0 0.5rem;
      max-width: 28rem;
      color: var(--color-text-muted);
    }
  `,
})
export class EmptyStateComponent {
  readonly icon = input('📝');
  readonly title = input.required<string>();
  readonly message = input('');
}
