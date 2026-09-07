import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { SESSION_KEY } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  let token = '';
  try { token = JSON.parse(localStorage.getItem(SESSION_KEY) ?? '{}').token ?? ''; } catch { token = ''; }
  const authenticatedRequest = token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
  const router = inject(Router);
  return next(authenticatedRequest).pipe(catchError((error: HttpErrorResponse) => {
    if (error.status === 401 && !request.url.endsWith('/login')) {
      localStorage.removeItem(SESSION_KEY);
      void router.navigate(['/login']);
    }
    return throwError(() => error);
  }));
};
