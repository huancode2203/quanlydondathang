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
  accounts: AccountPermission[];
}

export interface AccountPermission {
  id: number;
  username: string;
  fullName: string;
  roleId: number;
  roleCode: string;
  roleName: string;
  isSystemAdmin: boolean;
  usesCustomPermissions: boolean;
  permissionIds: number[];
}
