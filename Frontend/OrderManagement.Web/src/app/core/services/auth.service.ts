import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { tap } from 'rxjs';
import { UserSession } from '../models/auth.model';

export const SESSION_KEY = 'order-management-session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly sessionSignal = signal<UserSession | null>(this.restore());
  readonly user = this.sessionSignal.asReadonly();
  readonly isAuthenticated = computed(() => {
    const session = this.sessionSignal();
    return !!session && new Date(session.expiresAt).getTime() > Date.now();
  });

  login(username: string, password: string) {
    return this.http.post<UserSession>('http://localhost:5000/api/auth/login', { username, password }).pipe(
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

  hasPermission(code: string): boolean { return this.user()?.permissions.includes(code) ?? false; }

  hasAnyPermission(codes: string[]): boolean {
    const permissions = this.user()?.permissions ?? [];
    return codes.some(code => permissions.includes(code));
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
      const value = JSON.parse(raw) as UserSession;
      if (new Date(value.expiresAt).getTime() <= Date.now()) {
        localStorage.removeItem(SESSION_KEY);
        return null;
      }
      return value;
    } catch { return null; }
  }
}
