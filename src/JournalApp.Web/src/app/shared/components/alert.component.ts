import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-alert',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="alert"
      [class.alert--error]="kind() === 'error'"
      [attr.role]="kind() === 'error' ? 'alert' : 'status'"
    >
      <p class="alert__message">{{ message() }}</p>
      @if (retryLabel()) {
        <button type="button" class="btn btn--secondary btn--small" (click)="retry.emit()">
          {{ retryLabel() }}
        </button>
      }
    </div>
  `,
  styles: `
    .alert {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      gap: 0.75rem;
      padding: 0.875rem 1rem;
      border-radius: var(--radius);
      border: 1px solid var(--color-border);
      background: var(--color-surface-muted);
    }
    .alert--error {
      border-color: var(--color-danger-border);
      background: var(--color-danger-bg);
      color: var(--color-danger-text);
    }
    .alert__message {
      margin: 0;
    }
  `,
})
export class AlertComponent {
  readonly message = input.required<string>();
  readonly kind = input<'error' | 'info'>('error');
  /** When set, shows a button that emits `retry`. */
  readonly retryLabel = input<string>();
  readonly retry = output<void>();
}
