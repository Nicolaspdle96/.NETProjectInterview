import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  effect,
  input,
  output,
  viewChild,
} from '@angular/core';

/** Accessible confirmation dialog built on the native <dialog> element. */
@Component({
  selector: 'app-confirm-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <dialog
      #dialog
      class="dialog"
      aria-labelledby="confirm-dialog-title"
      aria-describedby="confirm-dialog-message"
      (cancel)="onCancel($event)"
    >
      <h2 id="confirm-dialog-title" class="dialog__title">{{ title() }}</h2>
      <p id="confirm-dialog-message" class="dialog__message">{{ message() }}</p>
      <div class="dialog__actions">
        <button
          type="button"
          class="btn btn--secondary"
          [disabled]="busy()"
          (click)="cancelled.emit()"
        >
          Cancel
        </button>
        <button
          type="button"
          class="btn btn--danger"
          [disabled]="busy()"
          (click)="confirmed.emit()"
        >
          {{ busy() ? 'Please wait…' : confirmLabel() }}
        </button>
      </div>
    </dialog>
  `,
  styles: `
    .dialog {
      width: min(28rem, calc(100vw - 2rem));
      padding: 1.5rem;
      border: none;
      border-radius: var(--radius-lg);
      background: var(--color-surface);
      color: var(--color-text);
      box-shadow: var(--shadow-lg);
    }
    .dialog::backdrop {
      background: rgb(20 16 12 / 0.45);
    }
    .dialog__title {
      margin: 0 0 0.5rem;
      font-size: 1.25rem;
    }
    .dialog__message {
      margin: 0 0 1.5rem;
      color: var(--color-text-muted);
    }
    .dialog__actions {
      display: flex;
      justify-content: flex-end;
      gap: 0.75rem;
    }
  `,
})
export class ConfirmDialogComponent {
  readonly open = input(false);
  readonly title = input('Are you sure?');
  readonly message = input('');
  readonly confirmLabel = input('Confirm');
  readonly busy = input(false);

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  constructor() {
    effect(() => {
      const element = this.dialog().nativeElement;
      if (this.open() && !element.open) {
        element.showModal();
      } else if (!this.open() && element.open) {
        element.close();
      }
    });
  }

  /** Escape key: keep the dialog state driven by the `open` input. */
  protected onCancel(event: Event): void {
    event.preventDefault();
    if (!this.busy()) {
      this.cancelled.emit();
    }
  }
}
