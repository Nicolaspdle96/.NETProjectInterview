import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-spinner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="spinner" role="status" aria-live="polite">
      <span class="spinner__circle" aria-hidden="true"></span>
      <span class="spinner__label">{{ label() }}</span>
    </div>
  `,
  styles: `
    .spinner {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.75rem;
      padding: 2rem 1rem;
      color: var(--color-text-muted);
    }
    .spinner__circle {
      width: 1.5rem;
      height: 1.5rem;
      border: 3px solid var(--color-border);
      border-top-color: var(--color-primary);
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }
    @keyframes spin {
      to {
        transform: rotate(360deg);
      }
    }
    @media (prefers-reduced-motion: reduce) {
      .spinner__circle {
        animation-duration: 2.4s;
      }
    }
  `,
})
export class SpinnerComponent {
  readonly label = input('Loading…');
}
