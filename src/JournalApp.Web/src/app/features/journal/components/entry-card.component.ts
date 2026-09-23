import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { JournalEntry } from '../../../domain/models/journal-entry';
import { ExcerptPipe } from '../../../shared/pipes/excerpt.pipe';
import { MoodBadgeComponent } from './mood-badge.component';

@Component({
  selector: 'app-entry-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, ExcerptPipe, MoodBadgeComponent],
  template: `
    <a
      class="entry-card card"
      [routerLink]="['/entries', entry().id]"
      [attr.data-mood]="entry().mood"
    >
      <div class="entry-card__meta">
        <time class="entry-card__date" [attr.datetime]="entry().createdAt.toISOString()">
          {{ entry().createdAt | date: 'EEE, MMM d, y · HH:mm' }}
        </time>
        <app-mood-badge [mood]="entry().mood" />
      </div>
      <h2 class="entry-card__title">{{ entry().title }}</h2>
      <p class="entry-card__excerpt">{{ entry().content | excerpt: 140 }}</p>
    </a>
  `,
  styles: `
    :host {
      display: block;
    }
    .entry-card {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      height: 100%;
      padding: 1.125rem 1.25rem;
      border-left: 4px solid var(--mood-accent, var(--color-border));
      color: inherit;
      text-decoration: none;
      transition:
        transform 0.15s ease,
        box-shadow 0.15s ease;
    }
    .entry-card:hover,
    .entry-card:focus-visible {
      transform: translateY(-2px);
      box-shadow: var(--shadow-lg);
    }
    .entry-card__meta {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      gap: 0.5rem;
    }
    .entry-card__date {
      font-size: 0.8125rem;
      color: var(--color-text-muted);
    }
    .entry-card__title {
      margin: 0;
      font-family: var(--font-serif);
      font-size: 1.2rem;
      line-height: 1.3;
      overflow-wrap: anywhere;
    }
    .entry-card__excerpt {
      margin: 0;
      color: var(--color-text-muted);
      overflow-wrap: anywhere;
    }
    @media (prefers-reduced-motion: reduce) {
      .entry-card {
        transition: none;
      }
    }
  `,
})
export class EntryCardComponent {
  readonly entry = input.required<JournalEntry>();
}
