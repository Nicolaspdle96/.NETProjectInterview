import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthStore } from '../../../core/auth/auth.store';
import { AppError } from '../../../domain/models/app-error';
import { USER_RULES } from '../../../domain/models/user';
import { AlertComponent } from '../../../shared/components/alert.component';
import { applyServerErrors, controlErrorMessage } from '../../../shared/forms/form-errors';
import {
  matchesPattern,
  notBlank,
  passwordStrength,
  trimmedLength,
} from '../../../shared/forms/validators';

type RegisterField = 'username' | 'email' | 'password';

@Component({
  selector: 'app-register-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, AlertComponent],
  templateUrl: './register-page.component.html',
  styleUrl: './auth-page.scss',
})
export class RegisterPageComponent {
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);

  protected readonly rules = USER_RULES;
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    username: [
      '',
      [notBlank, trimmedLength(USER_RULES.usernameMinLength, USER_RULES.usernameMaxLength)],
    ],
    email: ['', [notBlank, matchesPattern(USER_RULES.emailPattern, 'email')]],
    password: [
      '',
      [Validators.required, Validators.minLength(USER_RULES.passwordMinLength), passwordStrength],
    ],
  });

  protected errorFor(field: RegisterField, label: string): string | null {
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
      await this.authStore.register(this.form.getRawValue());
      await this.router.navigateByUrl('/entries');
    } catch (error) {
      const appError = AppError.from(error);
      applyServerErrors(this.form, appError.fieldErrors);
      this.errorMessage.set(appError.message);
    } finally {
      this.submitting.set(false);
    }
  }
}
