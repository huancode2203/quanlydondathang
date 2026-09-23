import { inject, Injectable } from '@angular/core';
import { AccountPermission, PermissionManagement, RolePermission } from '../models/permission.model';
import { ApiClient } from './api-client.service';

@Injectable({ providedIn: 'root' })
export class PermissionApiService {
  private readonly api = inject(ApiClient);

  getPermissions() {
    return this.api.post<PermissionManagement>('permissions/search');
  }

  updateRole(roleId: number, permissionIds: number[]) {
    return this.api.post<RolePermission>('permissions/roles/update', { id: roleId, permissionIds });
  }

  updateAccount(accountId: number, roleIds: number[], usesCustomPermissions: boolean, permissionIds: number[]) {
    return this.api.post<AccountPermission>('permissions/accounts/update', {
      id: accountId,
      roleIds,
      usesCustomPermissions,
      permissionIds,
    });
  }
}
