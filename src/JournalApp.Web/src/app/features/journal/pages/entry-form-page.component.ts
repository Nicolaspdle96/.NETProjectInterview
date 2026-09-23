import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import {
  FormControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AppError } from '../../../domain/models/app-error';
import { ENTRY_RULES, JournalEntry } from '../../../domain/models/journal-entry';
import { Mood } from '../../../domain/models/mood';
import { AlertComponent } from '../../../shared/components/alert.component';
import { SpinnerComponent } from '../../../shared/components/spinner.component';
import { applyServerErrors, controlErrorMessage } from '../../../shared/forms/form-errors';
import { notBlank, trimmedMaxLength } from '../../../shared/forms/validators';
import { EmptyStateComponent } from '../components/empty-state.component';
import { MoodPickerComponent } from '../components/mood-picker.component';
import { JournalStore } from '../state/journal.store';

/** Create (`/entries/new`) and edit (`/entries/:id/edit`) page. */
@Component({
  selector: 'app-entry-form-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    AlertComponent,
    SpinnerComponent,
    EmptyStateComponent,
    MoodPickerComponent,
  ],
  templateUrl: './entry-form-page.component.html',
  styleUrl: './entry-form-page.component.scss',
})
export class EntryFormPageComponent implements OnInit {
  private readonly store = inject(JournalStore);
  private readonly router = inject(Router);

  /** Bound from the `:id` route parameter; absent when creating. */
  readonly id = input<string>();

  protected readonly rules = ENTRY_RULES;
  protected readonly isEdit = computed(() => !!this.id());
  protected readonly loading = signal(false);
  protected readonly loadError = signal<AppError | null>(null);
  protected readonly submitting = signal(false);
  protected readonly submitError = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    title: ['', [notBlank, trimmedMaxLength(ENTRY_RULES.titleMaxLength)]],
    content: ['', [notBlank, Validators.maxLength(ENTRY_RULES.contentMaxLength)]],
    mood: new FormControl<Mood | null>(null),
  });

  ngOnInit(): void {
    const id = this.id();
    if (id) {
      const cached = this.store.cached(id);
      if (cached) {
        this.fill(cached);
      }
      void this.loadEntry(id);
    }
  }

  protected errorFor(field: 'title' | 'content', label: string): string | null {
    return controlErrorMessage(this.form.controls[field], label);
  }

  protected async loadEntry(id: string): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      const entry = await this.store.getById(id);
      if (!this.form.dirty) {
        this.fill(entry);
      }
    } catch (error) {
      this.loadError.set(AppError.from(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.submitError.set(null);
    try {
      const draft = this.form.getRawValue();
      const id = this.id();
      const saved = id ? await this.store.update(id, draft) : await this.store.create(draft);
      this.form.markAsPristine();
      await this.router.navigate(['/entries', saved.id]);
    } catch (error) {
      const appError = AppError.from(error);
      applyServerErrors(this.form, appError.fieldErrors);
      this.submitError.set(appError.message);
    } finally {
      this.submitting.set(false);
    }
  }

  private fill(entry: JournalEntry): void {
    this.form.reset({ title: entry.title, content: entry.content, mood: entry.mood });
  }
}
