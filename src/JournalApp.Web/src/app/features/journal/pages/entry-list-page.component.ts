import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AlertComponent } from '../../../shared/components/alert.component';
import { SpinnerComponent } from '../../../shared/components/spinner.component';
import { EmptyStateComponent } from '../components/empty-state.component';
import { EntryCardComponent } from '../components/entry-card.component';
import { JournalStore } from '../state/journal.store';

@Component({
  selector: 'app-entry-list-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, AlertComponent, SpinnerComponent, EmptyStateComponent, EntryCardComponent],
  template: `
    <section class="page">
      <header class="page__header">
        <div>
          <h1 class="page__title">Your journal</h1>
          @if (store.loaded() && !store.isEmpty()) {
            <p class="page__subtitle">
              {{ store.entries().length }} {{ store.entries().length === 1 ? 'entry' : 'entries' }}
            </p>
          }
        </div>
        <a class="btn btn--primary" routerLink="/entries/new">+ New entry</a>
      </header>

      @if (store.error(); as error) {
        <app-alert [message]="error.message" retryLabel="Try again" (retry)="reload()" />
      }

      @if (store.loading() && !store.loaded()) {
        <app-spinner label="Loading your entries…" />
      } @else if (store.isEmpty()) {
        <app-empty-state
          title="Your journal is empty"
          message="Write your first entry to start keeping a record of your days."
        >
          <a class="btn btn--primary" routerLink="/entries/new">Write your first entry</a>
        </app-empty-state>
      } @else {
        <ul class="entry-grid" [attr.aria-busy]="store.loading()">
          @for (entry of store.entries(); track entry.id) {
            <li><app-entry-card [entry]="entry" /></li>
          }
        </ul>
      }
    </section>
  `,
  styles: `
    .entry-grid {
      display: grid;
      grid-template-columns: 1fr;
      gap: 1rem;
      margin: 0;
      padding: 0;
      list-style: none;

      @media (min-width: 40rem) {
        grid-template-columns: repeat(2, minmax(0, 1fr));
      }
      @media (min-width: 64rem) {
        grid-template-columns: repeat(3, minmax(0, 1fr));
      }
    }
  `,
})
export class EntryListPageComponent implements OnInit {
  protected readonly store = inject(JournalStore);

  ngOnInit(): void {
    void this.store.load();
  }

  protected reload(): void {
    void this.store.load();
  }
}
