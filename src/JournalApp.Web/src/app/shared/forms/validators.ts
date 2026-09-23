import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Required, ignoring surrounding whitespace. */
export const notBlank: ValidatorFn = (control: AbstractControl<string | null>) =>
  (control.value ?? '').trim().length > 0 ? null : { required: true };

/** Max length measured after trimming, like the backend does for titles. */
export function trimmedMaxLength(max: number): ValidatorFn {
  return (control: AbstractControl<string | null>) => {
    const length = (control.value ?? '').trim().length;
    return length > max ? { maxlength: { requiredLength: max, actualLength: length } } : null;
  };
}

/** Trimmed length between min and max (inclusive). */
export function trimmedLength(min: number, max: number): ValidatorFn {
  return (control: AbstractControl<string | null>) => {
    const length = (control.value ?? '').trim().length;
    if (length === 0) {
      return null;
    }
    return length < min || length > max ? { length: { min, max, actualLength: length } } : null;
  };
}

export function matchesPattern(pattern: RegExp, errorKey: string): ValidatorFn {
  return (control: AbstractControl<string | null>) => {
    const value = (control.value ?? '').trim();
    return value.length === 0 || pattern.test(value) ? null : { [errorKey]: true };
  };
}

/** At least one uppercase letter, one lowercase letter and one digit. */
export const passwordStrength: ValidatorFn = (control: AbstractControl<string | null>) => {
  const value = control.value ?? '';
  if (value.length === 0) {
    return null;
  }
  const errors: ValidationErrors = {};
  if (!/[A-Z]/.test(value)) errors['uppercase'] = true;
  if (!/[a-z]/.test(value)) errors['lowercase'] = true;
  if (!/\d/.test(value)) errors['digit'] = true;
  return Object.keys(errors).length > 0 ? errors : null;
};
