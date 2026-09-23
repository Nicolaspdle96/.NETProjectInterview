import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AppError } from '../../domain/models/app-error';
import { AuthSession } from '../../domain/models/user';
import { AuthRepository } from '../../domain/repositories/auth.repository';
import { AuthStore } from './auth.store';

const futureSession = (): AuthSession => ({
  token: 'jwt-token',
  expiresAt: new Date(Date.now() + 60 * 60 * 1000),
  user: { id: 'u1', username: 'demo', email: 'demo@journal.com', createdAt: new Date() },
});

describe('AuthStore', () => {
  let repository: {
    login: ReturnType<typeof vi.fn>;
    register: ReturnType<typeof vi.fn>;
    me: ReturnType<typeof vi.fn>;
  };
  let router: { navigateByUrl: ReturnType<typeof vi.fn> };

  const createStore = () => {
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthRepository, useValue: repository },
        { provide: Router, useValue: router },
      ],
    });
    return TestBed.inject(AuthStore);
  };

  beforeEach(() => {
    localStorage.clear();
    repository = { login: vi.fn(), register: vi.fn(), me: vi.fn() };
    router = { navigateByUrl: vi.fn().mockResolvedValue(true) };
  });

  it('starts unauthenticated when nothing is stored', () => {
    const store = createStore();

    expect(store.isAuthenticated()).toBe(false);
    expect(store.token()).toBeNull();
  });

  it('stores the session after a successful login', async () => {
    repository.login.mockReturnValue(of(futureSession()));
    const store = createStore();

    await store.login({ email: 'demo@journal.com', password: 'Demo123!' });

    expect(store.isAuthenticated()).toBe(true);
    expect(store.user()?.username).toBe('demo');
    expect(localStorage.getItem('journal-app.session')).toContain('jwt-token');
  });

  it('propagates login errors and stays signed out', async () => {
    repository.login.mockReturnValue(throwError(() => new AppError('unauthorized', 'Invalid')));
    const store = createStore();

    await expect(store.login({ email: 'x@y.com', password: 'bad' })).rejects.toBeInstanceOf(
      AppError,
    );
    expect(store.isAuthenticated()).toBe(false);
  });

  it('restores a non-expired session from localStorage on startup', async () => {
    repository.login.mockReturnValue(of(futureSession()));
    await createStore().login({ email: 'demo@journal.com', password: 'Demo123!' });
    TestBed.resetTestingModule();

    const restored = createStore();

    expect(restored.isAuthenticated()).toBe(true);
    expect(restored.user()?.email).toBe('demo@journal.com');
  });

  it('discards an expired stored session', () => {
    localStorage.setItem(
      'journal-app.session',
      JSON.stringify({
        token: 'old',
        expiresAt: new Date(Date.now() - 1000).toISOString(),
        user: {
          id: 'u1',
          username: 'demo',
          email: 'demo@journal.com',
          createdAt: new Date().toISOString(),
        },
      }),
    );

    const store = createStore();

    expect(store.isAuthenticated()).toBe(false);
    expect(localStorage.getItem('journal-app.session')).toBeNull();
  });

  it('clears the session and redirects to /login on logout', async () => {
    repository.login.mockReturnValue(of(futureSession()));
    const store = createStore();
    await store.login({ email: 'demo@journal.com', password: 'Demo123!' });

    store.logout();

    expect(store.isAuthenticated()).toBe(false);
    expect(localStorage.getItem('journal-app.session')).toBeNull();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/login');
  });

  it('registers and then signs the new user in', async () => {
    repository.register.mockReturnValue(of(futureSession().user));
    repository.login.mockReturnValue(of(futureSession()));
    const store = createStore();

    await store.register({ username: 'demo', email: 'demo@journal.com', password: 'Demo123!' });

    expect(repository.login).toHaveBeenCalledWith({
      email: 'demo@journal.com',
      password: 'Demo123!',
    });
    expect(store.isAuthenticated()).toBe(true);
  });
});
