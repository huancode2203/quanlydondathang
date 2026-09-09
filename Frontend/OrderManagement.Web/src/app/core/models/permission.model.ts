export interface Permission {
  id: number;
  code: string;
  name: string;
  feature: string;
  action: string;
  description?: string;
}

export interface RolePermission {
  id: number;
  code: string;
  name: string;
  description?: string;
  isSystemAdmin: boolean;
  permissionIds: number[];
}

export interface PermissionManagement {
  roles: RolePermission[];
  permissions: Permission[];
}
