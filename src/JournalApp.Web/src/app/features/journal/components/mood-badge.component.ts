import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Mood, moodOption } from '../../../domain/models/mood';

@Component({
  selector: 'app-mood-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (option(); as o) {
      <span class="mood-badge" [attr.data-mood]="o.value">
        <span aria-hidden="true">{{ o.emoji }}</span>
        {{ o.label }}
      </span>
    }
  `,
  styles: `
    .mood-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
      padding: 0.2rem 0.6rem;
      border-radius: 999px;
      font-size: 0.8125rem;
      font-weight: 600;
      background: var(--mood-bg, var(--color-surface-muted));
      color: var(--mood-text, var(--color-text));
      white-space: nowrap;
    }
  `,
})
export class MoodBadgeComponent {
  readonly mood = input<Mood | null>(null);
  protected readonly option = computed(() => {
    const mood = this.mood();
    return mood ? moodOption(mood) : null;
  });
}
