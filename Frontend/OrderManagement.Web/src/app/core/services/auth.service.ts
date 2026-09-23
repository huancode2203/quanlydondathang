import { inject, Injectable, signal } from '@angular/core';
import { tap } from 'rxjs';
import { UserSession } from '../models/auth.model';
import { ApiClient } from './api-client.service';

export const SESSION_KEY = 'order-management-session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiClient);
  private readonly sessionSignal = signal<UserSession | null>(this.restore());
  readonly user = this.sessionSignal.asReadonly();
  isAuthenticated(): boolean {
    const session = this.sessionSignal();
    return !!session && new Date(session.expiresAt).getTime() > Date.now();
  }

  login(username: string, password: string) {
    return this.api.post<UserSession>('auth/login', { username, password }).pipe(
      tap(session => {
        localStorage.setItem(SESSION_KEY, JSON.stringify(session));
        this.sessionSignal.set(session);
      }),
    );
  }

  logout(): void {
    localStorage.removeItem(SESSION_KEY);
    this.sessionSignal.set(null);
  }

  hasPermission(code: string): boolean { return this.isAuthenticated() && (this.user()?.permissions.includes(code) ?? false); }

  hasAnyPermission(codes: string[]): boolean {
    const permissions = this.user()?.permissions ?? [];
    return this.isAuthenticated() && codes.some(code => permissions.includes(code));
  }

  defaultRoute(): string {
    if (this.hasPermission('ORDER_VIEW')) return '/orders';
    if (this.hasPermission('CUSTOMER_VIEW')) return '/customers';
    if (this.hasPermission('PRODUCT_VIEW')) return '/products';
    if (this.hasPermission('PERMISSION_MANAGE')) return '/permissions';
    return '/no-access';
  }

  private restore(): UserSession | null {
    try {
      const raw = localStorage.getItem(SESSION_KEY);
      if (!raw) return null;
      const value = JSON.parse(raw) as Partial<UserSession> | null;
      if (!value || typeof value.token !== 'string' || !value.token
        || typeof value.expiresAt !== 'string'
        || !Number.isFinite(Date.parse(value.expiresAt))
        || Date.parse(value.expiresAt) <= Date.now()
        || !Array.isArray(value.permissions)
        || !value.permissions.every(permission => typeof permission === 'string')) {
        localStorage.removeItem(SESSION_KEY);
        return null;
      }
      return value as UserSession;
    } catch { return null; }
  }
}
