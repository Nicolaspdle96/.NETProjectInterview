import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AppError } from '../../../domain/models/app-error';
import { JournalEntry } from '../../../domain/models/journal-entry';
import { AlertComponent } from '../../../shared/components/alert.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog.component';
import { SpinnerComponent } from '../../../shared/components/spinner.component';
import { EmptyStateComponent } from '../components/empty-state.component';
import { MoodBadgeComponent } from '../components/mood-badge.component';
import { JournalStore } from '../state/journal.store';

@Component({
  selector: 'app-entry-detail-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    RouterLink,
    AlertComponent,
    ConfirmDialogComponent,
    SpinnerComponent,
    EmptyStateComponent,
    MoodBadgeComponent,
  ],
  templateUrl: './entry-detail-page.component.html',
  styleUrl: './entry-detail-page.component.scss',
})
export class EntryDetailPageComponent implements OnInit {
  private readonly store = inject(JournalStore);
  private readonly router = inject(Router);

  /** Bound from the `:id` route parameter. */
  readonly id = input.required<string>();

  protected readonly entry = signal<JournalEntry | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<AppError | null>(null);
  protected readonly confirmOpen = signal(false);
  protected readonly deleting = signal(false);
  protected readonly deleteError = signal<string | null>(null);

  ngOnInit(): void {
    this.entry.set(this.store.cached(this.id()) ?? null);
    void this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.entry.set(await this.store.getById(this.id()));
    } catch (error) {
      this.entry.set(null);
      this.error.set(AppError.from(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected askDelete(): void {
    this.deleteError.set(null);
    this.confirmOpen.set(true);
  }

  protected cancelDelete(): void {
    this.confirmOpen.set(false);
  }

  protected async confirmDelete(): Promise<void> {
    this.deleting.set(true);
    try {
      await this.store.remove(this.id());
      this.confirmOpen.set(false);
      await this.router.navigateByUrl('/entries');
    } catch (error) {
      this.confirmOpen.set(false);
      this.deleteError.set(AppError.from(error).message);
    } finally {
      this.deleting.set(false);
    }
  }
}
