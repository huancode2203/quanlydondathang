import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const permissionManagementGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) return router.createUrlTree(['/login']);
  return auth.hasPermission('PERMISSION_MANAGE')
    ? true
    : router.createUrlTree(['/orders']);
};
