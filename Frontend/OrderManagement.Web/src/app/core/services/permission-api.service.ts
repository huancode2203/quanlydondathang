import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { PermissionManagement, RolePermission } from '../models/permission.model';

@Injectable({ providedIn: 'root' })
export class PermissionApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5000/api/permissions';

  getPermissions() {
    return this.http.get<PermissionManagement>(this.apiUrl);
  }

  updateRole(roleId: number, permissionIds: number[]) {
    return this.http.put<RolePermission>(`${this.apiUrl}/roles/${roleId}`, { permissionIds });
  }
}
