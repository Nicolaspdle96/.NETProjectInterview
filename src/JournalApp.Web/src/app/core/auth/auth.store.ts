import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthSession, LoginCredentials, Registration, User } from '../../domain/models/user';
import { AuthRepository } from '../../domain/repositories/auth.repository';
import { sessionStorageAdapter } from './session-storage';

// setTimeout overflows above ~24.8 days; tokens are much shorter-lived anyway.
const MAX_TIMEOUT_MS = 2_147_483_647;

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly repository = inject(AuthRepository);
  private readonly router = inject(Router);
  private expiryTimer: ReturnType<typeof setTimeout> | undefined;

  private readonly session = signal<AuthSession | null>(null);

  readonly user = computed<User | null>(() => this.session()?.user ?? null);
  readonly token = computed<string | null>(() => this.session()?.token ?? null);
  readonly isAuthenticated = computed(() => this.session() !== null);

  constructor() {
    this.restore();
    inject(DestroyRef).onDestroy(() => clearTimeout(this.expiryTimer));
  }

  async login(credentials: LoginCredentials): Promise<void> {
    const session = await firstValueFrom(this.repository.login(credentials));
    this.setSession(session);
  }

  /** Registers the account and signs the new user in. */
  async register(registration: Registration): Promise<void> {
    await firstValueFrom(this.repository.register(registration));
    await this.login({ email: registration.email, password: registration.password });
  }

  logout(): void {
    clearTimeout(this.expiryTimer);
    this.session.set(null);
    sessionStorageAdapter.clear();
    void this.router.navigateByUrl('/login');
  }

  private restore(): void {
    const stored = sessionStorageAdapter.read();
    if (stored && stored.expiresAt.getTime() > Date.now()) {
      this.setSession(stored);
    } else {
      sessionStorageAdapter.clear();
    }
  }

  private setSession(session: AuthSession): void {
    this.session.set(session);
    sessionStorageAdapter.write(session);
    this.scheduleExpiry(session.expiresAt);
  }

  private scheduleExpiry(expiresAt: Date): void {
    clearTimeout(this.expiryTimer);
    const remaining = Math.min(expiresAt.getTime() - Date.now(), MAX_TIMEOUT_MS);
    this.expiryTimer = setTimeout(() => this.logout(), Math.max(remaining, 0));
  }
}
