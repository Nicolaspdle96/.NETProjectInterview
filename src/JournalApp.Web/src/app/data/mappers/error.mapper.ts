import { HttpErrorResponse } from '@angular/common/http';
import { OperatorFunction, catchError, throwError } from 'rxjs';
import { AppError, AppErrorKind } from '../../domain/models/app-error';
import { ProblemDetailsDto } from '../dtos/problem-details.dto';

const DEFAULT_MESSAGES: Record<AppErrorKind, string> = {
  validation: 'Please review the highlighted fields.',
  unauthorized: 'Your session is not valid. Please sign in again.',
  notFound: 'The requested item was not found.',
  conflict: 'That value is already in use.',
  network: 'Unable to reach the server. Check your connection and try again.',
  unknown: 'Something went wrong. Please try again.',
};

/** RxJS operator that converts transport errors into {@link AppError}. */
export function mapHttpErrors<T>(): OperatorFunction<T, T> {
  return catchError((error: unknown) => throwError(() => toAppError(error)));
}

export function toAppError(error: unknown): AppError {
  if (!(error instanceof HttpErrorResponse)) {
    return AppError.from(error);
  }

  const kind = kindFromStatus(error.status);
  const problem = isProblemDetails(error.error) ? error.error : undefined;

  return new AppError(
    kind,
    kind === 'unknown' || kind === 'network'
      ? DEFAULT_MESSAGES[kind]
      : (problem?.detail ?? DEFAULT_MESSAGES[kind]),
    normalizeFieldErrors(problem?.errors),
  );
}

function kindFromStatus(status: number): AppErrorKind {
  switch (status) {
    case 0:
      return 'network';
    case 400:
      return 'validation';
    case 401:
      return 'unauthorized';
    case 404:
      return 'notFound';
    case 409:
      return 'conflict';
    default:
      return 'unknown';
  }
}

function isProblemDetails(value: unknown): value is ProblemDetailsDto {
  return typeof value === 'object' && value !== null && ('title' in value || 'errors' in value);
}

/** Server keys can be `Title`, `title` or `$.title`; normalize them to camelCase field names. */
function normalizeFieldErrors(
  errors: Record<string, string[]> | undefined,
): Record<string, string[]> {
  const result: Record<string, string[]> = {};
  for (const [key, messages] of Object.entries(errors ?? {})) {
    const field = key.replace(/^\$\./, '');
    const normalized = field.charAt(0).toLowerCase() + field.slice(1);
    result[normalized] = [...(result[normalized] ?? []), ...messages];
  }
  return result;
}
