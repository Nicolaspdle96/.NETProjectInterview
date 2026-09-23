export type AppErrorKind =
  'validation' | 'unauthorized' | 'notFound' | 'conflict' | 'network' | 'unknown';

/**
 * Error shape used across the app, independent of the transport.
 * `fieldErrors` keys are camelCase field names (e.g. `title`, `email`).
 */
export class AppError extends Error {
  constructor(
    readonly kind: AppErrorKind,
    message: string,
    readonly fieldErrors: Readonly<Record<string, readonly string[]>> = {},
  ) {
    super(message);
    this.name = 'AppError';
  }

  static from(error: unknown): AppError {
    return error instanceof AppError
      ? error
      : new AppError('unknown', 'Something went wrong. Please try again.');
  }
}
