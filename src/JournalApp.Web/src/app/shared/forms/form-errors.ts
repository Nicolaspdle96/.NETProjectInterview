import { AbstractControl, FormGroup } from '@angular/forms';

/** Human-readable message for the first error of a control, or null when valid/untouched. */
export function controlErrorMessage(control: AbstractControl | null, label: string): string | null {
  if (!control || !control.errors || !(control.touched || control.dirty)) {
    return null;
  }
  const errors = control.errors;

  if (errors['server']) return errors['server'] as string;
  if (errors['required']) return `${label} is required.`;
  if (errors['maxlength']) {
    return `${label} must be at most ${(errors['maxlength'] as { requiredLength: number }).requiredLength} characters.`;
  }
  if (errors['minlength']) {
    return `${label} must be at least ${(errors['minlength'] as { requiredLength: number }).requiredLength} characters.`;
  }
  if (errors['length']) {
    const { min, max } = errors['length'] as { min: number; max: number };
    return `${label} must be between ${min} and ${max} characters.`;
  }
  if (errors['email']) return 'Enter a valid email address.';
  if (errors['uppercase']) return `${label} must contain at least one uppercase letter.`;
  if (errors['lowercase']) return `${label} must contain at least one lowercase letter.`;
  if (errors['digit']) return `${label} must contain at least one digit.`;
  return `${label} is not valid.`;
}

/** Shows server-side field errors (camelCase keys) on the matching form controls. */
export function applyServerErrors(
  form: FormGroup,
  fieldErrors: Readonly<Record<string, readonly string[]>>,
): void {
  for (const [field, messages] of Object.entries(fieldErrors)) {
    const control = form.get(field);
    if (control && messages.length > 0) {
      control.setErrors({ ...control.errors, server: messages[0] });
      control.markAsTouched();
    }
  }
}
