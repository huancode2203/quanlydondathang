import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const permissionGuard = (permissionCode: string): CanActivateFn => () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) return router.createUrlTree(['/login']);
  return auth.hasPermission(permissionCode)
    ? true
    : router.createUrlTree([auth.defaultRoute()]);
};

export const permissionManagementGuard = permissionGuard('PERMISSION_MANAGE');
