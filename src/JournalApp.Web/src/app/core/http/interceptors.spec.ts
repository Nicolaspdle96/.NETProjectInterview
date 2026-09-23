import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { AuthStore } from '../auth/auth.store';
import { authInterceptor } from './auth.interceptor';
import { errorInterceptor } from './error.interceptor';

describe('HTTP interceptors', () => {
  const token = signal<string | null>(null);
  const logout = vi.fn();
  let http: HttpClient;
  let controller: HttpTestingController;

  beforeEach(() => {
    token.set(null);
    logout.mockReset();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthStore, useValue: { token, logout } },
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  it('adds the bearer token to API requests', () => {
    token.set('abc');

    void firstValueFrom(http.get('/api/entries'));

    const request = controller.expectOne('/api/entries');
    expect(request.request.headers.get('Authorization')).toBe('Bearer abc');
    request.flush([]);
  });

  it('does not add the token to non-API requests', () => {
    token.set('abc');

    void firstValueFrom(http.get('/assets/data.json'));

    const request = controller.expectOne('/assets/data.json');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });

  it('logs out when an authenticated request returns 401', async () => {
    token.set('expired');

    const result = firstValueFrom(http.get('/api/entries'));
    controller.expectOne('/api/entries').flush(null, { status: 401, statusText: 'Unauthorized' });

    await expect(result).rejects.toBeTruthy();
    expect(logout).toHaveBeenCalled();
  });

  it('does not log out when an anonymous request (e.g. login) returns 401', async () => {
    const result = firstValueFrom(http.post('/api/auth/login', {}));
    controller
      .expectOne('/api/auth/login')
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    await expect(result).rejects.toBeTruthy();
    expect(logout).not.toHaveBeenCalled();
  });
});
