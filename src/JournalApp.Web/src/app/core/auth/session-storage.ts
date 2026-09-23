import { AuthSession } from '../../domain/models/user';

const STORAGE_KEY = 'journal-app.session';

interface StoredSession {
  token: string;
  expiresAt: string;
  user: { id: string; username: string; email: string; createdAt: string };
}

/** Persists the session (JWT + user) in localStorage so it survives page reloads. */
export const sessionStorageAdapter = {
  read(): AuthSession | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) {
        return null;
      }
      const stored = JSON.parse(raw) as StoredSession;
      return {
        token: stored.token,
        expiresAt: new Date(stored.expiresAt),
        user: { ...stored.user, createdAt: new Date(stored.user.createdAt) },
      };
    } catch {
      return null;
    }
  },

  write(session: AuthSession): void {
    try {
      const stored: StoredSession = {
        token: session.token,
        expiresAt: session.expiresAt.toISOString(),
        user: { ...session.user, createdAt: session.user.createdAt.toISOString() },
      };
      localStorage.setItem(STORAGE_KEY, JSON.stringify(stored));
    } catch {
      // Storage may be unavailable (private mode, quota); the session still works in memory.
    }
  },

  clear(): void {
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      // Ignore: nothing to clear if storage is unavailable.
    }
  },
};
