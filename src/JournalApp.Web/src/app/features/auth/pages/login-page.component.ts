import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthStore } from '../../../core/auth/auth.store';
import { AppError } from '../../../domain/models/app-error';
import { AlertComponent } from '../../../shared/components/alert.component';
import { controlErrorMessage } from '../../../shared/forms/form-errors';

@Component({
  selector: 'app-login-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, AlertComponent],
  templateUrl: './login-page.component.html',
  styleUrl: './auth-page.scss',
})
export class LoginPageComponent {
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);

  /** Bound from the `?returnUrl=` query parameter. */
  readonly returnUrl = input<string>();

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required]],
    password: ['', [Validators.required]],
  });

  protected errorFor(field: 'email' | 'password', label: string): string | null {
    return controlErrorMessage(this.form.controls[field], label);
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    try {
      await this.authStore.login(this.form.getRawValue());
      await this.router.navigateByUrl(this.safeReturnUrl());
    } catch (error) {
      const appError = AppError.from(error);
      this.errorMessage.set(
        appError.kind === 'unauthorized' ? 'Invalid email or password.' : appError.message,
      );
    } finally {
      this.submitting.set(false);
    }
  }

  /** Only allow in-app paths to avoid open redirects. */
  private safeReturnUrl(): string {
    const url = this.returnUrl();
    return url && url.startsWith('/') && !url.startsWith('//') ? url : '/entries';
  }
}
