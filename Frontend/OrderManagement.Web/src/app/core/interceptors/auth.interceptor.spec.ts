import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../services/api-client.service';
import { AuthService, SESSION_KEY } from '../services/auth.service';
import { authInterceptor } from './auth.interceptor';

describe('Authentication with the POST API', () => {
  let http: HttpTestingController;
  let navigate: ReturnType<typeof vi.fn>;
  const session = () => ({
    token: 'test-token', expiresAt: new Date(Date.now() + 60_000).toISOString(),
    employeeId: 1, username: 'test', fullName: 'Test user', roleName: 'Sales', permissions: ['ORDER_VIEW'],
  });

  beforeEach(() => {
    localStorage.setItem(SESSION_KEY, JSON.stringify(session()));
    navigate = vi.fn().mockResolvedValue(true);
    TestBed.configureTestingModule({ providers: [
      provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting(),
      { provide: Router, useValue: { navigate } },
    ] });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.removeItem(SESSION_KEY);
    vi.restoreAllMocks();
  });

  it('attaches the token and clears both stored and in-memory session on 401', async () => {
    const auth = TestBed.inject(AuthService);
    const result = firstValueFrom(TestBed.inject(ApiClient).post('orders/search')).catch(error => error);
    const request = http.expectOne('/api/orders/search');
    expect(request.request.headers.get('Authorization')).toBe('Bearer test-token');
    request.flush({ status: 401, value: null, message: 'Hết phiên.' }, { status: 401, statusText: 'Unauthorized' });
    await result;
    expect(auth.user()).toBeNull();
    expect(localStorage.getItem(SESSION_KEY)).toBeNull();
    expect(navigate).toHaveBeenCalledWith(['/login']);
  });

  it('keeps the session on 403', async () => {
    const result = firstValueFrom(TestBed.inject(ApiClient).post('permissions/search')).catch(error => error);
    http.expectOne('/api/permissions/search').flush({ status: 403, value: null, message: 'Không có quyền.' },
      { status: 403, statusText: 'Forbidden' });
    await result;
    expect(TestBed.inject(AuthService).isAuthenticated()).toBe(true);
    expect(navigate).not.toHaveBeenCalled();
  });

  it('never sends a session token to login or an external URL', () => {
    TestBed.inject(ApiClient).post('auth/login').subscribe();
    const login = http.expectOne('/api/auth/login');
    expect(login.request.headers.has('Authorization')).toBe(false);
    login.flush({ status: 200, value: null, message: '' });
    TestBed.inject(HttpClient).post('https://example.test/api', {}).subscribe();
    const external = http.expectOne('https://example.test/api');
    expect(external.request.headers.has('Authorization')).toBe(false);
    external.flush({});
  });

  it('rechecks expiry after a session has already been used', () => {
    const auth = TestBed.inject(AuthService);
    expect(auth.isAuthenticated()).toBe(true);
    vi.spyOn(Date, 'now').mockReturnValue(Date.now() + 120_000);
    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.hasPermission('ORDER_VIEW')).toBe(false);
  });

  it('discards malformed saved sessions', () => {
    localStorage.setItem(SESSION_KEY, JSON.stringify({ expiresAt: 'not-a-date', permissions: null }));
    expect(TestBed.inject(AuthService).isAuthenticated()).toBe(false);
    expect(localStorage.getItem(SESSION_KEY)).toBeNull();
  });
});
