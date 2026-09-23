import { FormControl } from '@angular/forms';
import { controlErrorMessage } from './forms/form-errors';
import { notBlank, passwordStrength, trimmedLength, trimmedMaxLength } from './forms/validators';
import { ExcerptPipe } from './pipes/excerpt.pipe';

describe('validators', () => {
  it('notBlank rejects whitespace-only values', () => {
    expect(notBlank(new FormControl('   '))).toEqual({ required: true });
    expect(notBlank(new FormControl(' a '))).toBeNull();
  });

  it('trimmedMaxLength measures the trimmed value', () => {
    const validator = trimmedMaxLength(5);

    expect(validator(new FormControl('  abcde  '))).toBeNull();
    expect(validator(new FormControl('abcdef'))).not.toBeNull();
  });

  it('trimmedLength enforces min and max', () => {
    const validator = trimmedLength(3, 30);

    expect(validator(new FormControl('ab'))).not.toBeNull();
    expect(validator(new FormControl('abc'))).toBeNull();
    expect(validator(new FormControl('a'.repeat(31)))).not.toBeNull();
  });

  it('passwordStrength requires upper, lower and digit', () => {
    expect(passwordStrength(new FormControl('Password1'))).toBeNull();
    expect(passwordStrength(new FormControl('password1'))).toEqual({ uppercase: true });
    expect(passwordStrength(new FormControl('PASSWORD1'))).toEqual({ lowercase: true });
    expect(passwordStrength(new FormControl('Password'))).toEqual({ digit: true });
  });
});

describe('controlErrorMessage', () => {
  it('returns null for untouched controls', () => {
    const control = new FormControl('', notBlank);

    expect(controlErrorMessage(control, 'Title')).toBeNull();
  });

  it('describes the first error of a touched control', () => {
    const control = new FormControl('', notBlank);
    control.markAsTouched();

    expect(controlErrorMessage(control, 'Title')).toBe('Title is required.');
  });

  it('prefers server errors', () => {
    const control = new FormControl('taken');
    control.setErrors({ server: 'The email is already in use.' });
    control.markAsTouched();

    expect(controlErrorMessage(control, 'Email')).toBe('The email is already in use.');
  });
});

describe('ExcerptPipe', () => {
  const pipe = new ExcerptPipe();

  it('returns short text untouched but collapses whitespace', () => {
    expect(pipe.transform('Hello\n\n  world', 50)).toBe('Hello world');
  });

  it('truncates long text at a word boundary with an ellipsis', () => {
    expect(pipe.transform('The quick brown fox jumps over the lazy dog', 20)).toBe(
      'The quick brown fox…',
    );
  });
});
