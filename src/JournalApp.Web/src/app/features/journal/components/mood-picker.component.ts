import {
  ChangeDetectionStrategy,
  Component,
  forwardRef,
  input,
  output,
  signal,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MOOD_OPTIONS, Mood } from '../../../domain/models/mood';

/**
 * Presentational mood selector usable as a reactive form control.
 * Clicking the selected mood again clears it (mood is optional).
 */
@Component({
  selector: 'app-mood-picker',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => MoodPickerComponent),
      multi: true,
    },
  ],
  template: `
    <div class="mood-picker" role="radiogroup" [attr.aria-labelledby]="labelledBy()">
      @for (option of options; track option.value) {
        <button
          type="button"
          role="radio"
          class="mood-picker__option"
          [attr.data-mood]="option.value"
          [attr.aria-checked]="value() === option.value"
          [disabled]="disabled()"
          (click)="select(option.value)"
        >
          <span class="mood-picker__emoji" aria-hidden="true">{{ option.emoji }}</span>
          <span class="mood-picker__label">{{ option.label }}</span>
        </button>
      }
    </div>
  `,
  styles: `
    .mood-picker {
      display: grid;
      grid-template-columns: repeat(5, minmax(0, 1fr));
      gap: 0.5rem;
    }
    .mood-picker__option {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.25rem;
      padding: 0.6rem 0.25rem;
      border: 2px solid var(--color-border);
      border-radius: var(--radius);
      background: var(--color-surface);
      color: var(--color-text);
      font: inherit;
      cursor: pointer;
      transition:
        border-color 0.15s ease,
        background 0.15s ease;
    }
    .mood-picker__option:hover:not(:disabled) {
      border-color: var(--mood-accent, var(--color-primary));
    }
    .mood-picker__option[aria-checked='true'] {
      border-color: var(--mood-accent, var(--color-primary));
      background: var(--mood-bg, var(--color-surface-muted));
    }
    .mood-picker__option:disabled {
      cursor: not-allowed;
      opacity: 0.6;
    }
    .mood-picker__emoji {
      font-size: 1.5rem;
    }
    .mood-picker__label {
      font-size: 0.75rem;
      font-weight: 600;
    }
  `,
})
export class MoodPickerComponent implements ControlValueAccessor {
  /** Id of the element labelling the group. */
  readonly labelledBy = input<string>();
  readonly moodChange = output<Mood | null>();

  protected readonly options = MOOD_OPTIONS;
  protected readonly value = signal<Mood | null>(null);
  protected readonly disabled = signal(false);

  private onChange: (value: Mood | null) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  protected select(mood: Mood): void {
    const next = this.value() === mood ? null : mood;
    this.value.set(next);
    this.onChange(next);
    this.onTouched();
    this.moodChange.emit(next);
  }

  writeValue(value: Mood | null): void {
    this.value.set(value ?? null);
  }

  registerOnChange(fn: (value: Mood | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }
}
