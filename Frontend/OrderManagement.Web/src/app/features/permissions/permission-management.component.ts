import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { AccountPermission, Permission, PermissionManagement, RolePermission } from '../../core/models/permission.model';
import { PermissionApiService } from '../../core/services/permission-api.service';

interface PermissionGroup {
  feature: string;
  label: string;
  description: string;
  permissions: Permission[];
}

@Component({
  selector: 'app-permission-management',
  standalone: true,
  templateUrl: './permission-management.component.html',
})
export class PermissionManagementComponent {
  private readonly api = inject(PermissionApiService);

  readonly data = signal<PermissionManagement | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  readonly accountKeyword = signal('');
  readonly selectedAccountId = signal<number | null>(null);
  readonly selectedAccountRoleIds = signal<ReadonlySet<number>>(new Set<number>());
  readonly selectedAccountPermissionIds = signal<ReadonlySet<number>>(new Set<number>());
  readonly accountUsesCustomPermissions = signal(false);
  readonly accountDirty = signal(false);
  readonly roleEditorOpen = signal(false);
  readonly editingRoleId = signal<number | null>(null);
  readonly roleEditorPermissionIds = signal<ReadonlySet<number>>(new Set<number>());
  readonly roleEditorDirty = signal(false);

  readonly roles = computed(() => this.data()?.roles ?? []);
  readonly accounts = computed(() => this.data()?.accounts ?? []);
  readonly filteredAccounts = computed(() => {
    const keyword = this.accountKeyword().trim().toLocaleLowerCase('vi');
    if (!keyword) return this.accounts();
    return this.accounts().filter(account =>
      `${account.fullName} ${account.username} ${account.roleNames.join(' ')}`.toLocaleLowerCase('vi').includes(keyword),
    );
  });
  readonly selectedAccount = computed(() => this.accounts().find(account => account.id === this.selectedAccountId()) ?? null);
  readonly selectedAccountRoles = computed(() => this.roles().filter(role => this.selectedAccountRoleIds().has(role.id)));
  readonly accountIsAdmin = computed(() => this.selectedAccountRoles().some(role => role.isSystemAdmin));
  readonly editingRole = computed(() => this.roles().find(role => role.id === this.editingRoleId()) ?? null);
  readonly selectedAccountIsLastAdmin = computed(() => {
    const account = this.selectedAccount();
    return !!account?.isSystemAdmin && this.accounts().filter(x => x.isSystemAdmin).length <= 1;
  });
  readonly groups = computed<PermissionGroup[]>(() => {
    const grouped = new Map<string, Permission[]>();
    for (const permission of this.data()?.permissions ?? []) {
      const feature = permission.feature || 'OTHER';
      grouped.set(feature, [...(grouped.get(feature) ?? []), permission]);
    }
    return [...grouped.entries()].map(([feature, permissions]) => ({
      feature,
      label: this.featureLabel(feature),
      description: this.featureDescription(feature),
      permissions,
    }));
  });

  constructor() { this.load(); }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.api.getPermissions().pipe(finalize(() => this.loading.set(false))).subscribe({
      next: data => {
        this.data.set(data);
        const current = data.accounts.find(account => account.id === this.selectedAccountId()) ?? data.accounts[0];
        if (current) this.applyAccount(current);
      },
      error: error => this.error.set(this.readError(error)),
    });
  }

  selectAccount(account: AccountPermission): void {
    if (this.saving() || account.id === this.selectedAccountId()) return;
    if (this.accountDirty() && !confirm('Bạn có thay đổi chưa lưu. Chuyển tài khoản và bỏ các thay đổi này?')) return;
    this.applyAccount(account);
    this.error.set('');
    this.notice.set('');
  }

  isAccountRoleSelected(roleId: number): boolean { return this.selectedAccountRoleIds().has(roleId); }

  toggleAccountRole(role: RolePermission): void {
    if (this.saving()) return;
    const next = new Set(this.selectedAccountRoleIds());
    if (next.has(role.id)) {
      if (next.size === 1) {
        this.error.set('Tài khoản phải có ít nhất một nhóm quyền.');
        return;
      }
      if (role.isSystemAdmin && this.selectedAccountIsLastAdmin()) {
        this.error.set('Phải giữ lại ít nhất một tài khoản thuộc nhóm Quản trị viên.');
        return;
      }
      next.delete(role.id);
    } else {
      next.add(role.id);
    }

    this.selectedAccountRoleIds.set(next);
    this.accountUsesCustomPermissions.set(false);
    this.selectedAccountPermissionIds.set(new Set(this.permissionsForRoles(next)));
    this.refreshAccountDirty();
    this.error.set('');
    this.notice.set('');
  }

  toggleAccountPermission(permissionId: number): void {
    if (this.saving() || this.accountIsAdmin() || !this.accountUsesCustomPermissions()) return;
    this.selectedAccountPermissionIds.set(this.toggleWithDependencies(this.selectedAccountPermissionIds(), permissionId));
    this.refreshAccountDirty();
    this.notice.set('');
  }

  enableCustomPermissions(): void {
    if (this.saving() || this.accountIsAdmin() || this.accountUsesCustomPermissions()) return;
    this.accountUsesCustomPermissions.set(true);
    this.refreshAccountDirty();
    this.notice.set('');
  }

  resetAccountToRoles(): void {
    if (this.accountIsAdmin() || this.saving() || !this.accountUsesCustomPermissions()) return;
    this.selectedAccountPermissionIds.set(new Set(this.permissionsForRoles(this.selectedAccountRoleIds())));
    this.accountUsesCustomPermissions.set(false);
    this.refreshAccountDirty();
    this.notice.set('');
  }

  selectAllAccountPermissions(): void {
    if (this.saving() || this.accountIsAdmin() || !this.accountUsesCustomPermissions()) return;
    this.selectedAccountPermissionIds.set(new Set((this.data()?.permissions ?? []).map(x => x.id)));
    this.refreshAccountDirty();
  }

  clearAccountPermissions(): void {
    if (this.saving() || this.accountIsAdmin() || !this.accountUsesCustomPermissions()) return;
    this.selectedAccountPermissionIds.set(new Set<number>());
    this.refreshAccountDirty();
  }

  saveAccount(): void {
    const account = this.selectedAccount();
    const roleIds = [...this.selectedAccountRoleIds()].sort((a, b) => a - b);
    if (!account || roleIds.length === 0 || !this.accountDirty() || this.saving()) return;

    this.saving.set(true);
    this.error.set('');
    const permissionIds = [...this.selectedAccountPermissionIds()].sort((a, b) => a - b);
    this.api.updateAccount(account.id, roleIds, this.accountUsesCustomPermissions(), permissionIds)
      .pipe(finalize(() => this.saving.set(false))).subscribe({
        next: updated => {
          const current = this.data();
          if (current) this.data.set({ ...current, accounts: current.accounts.map(item => item.id === updated.id ? updated : item) });
          this.applyAccount(updated);
          this.notice.set(`Đã cập nhật quyền cho tài khoản ${updated.username}.`);
        },
        error: error => this.error.set(this.readError(error)),
      });
  }

  isAccountPermissionGranted(permissionId: number): boolean { return this.selectedAccountPermissionIds().has(permissionId); }
  accountGroupGrantedCount(group: PermissionGroup): number {
    return group.permissions.filter(permission => this.isAccountPermissionGranted(permission.id)).length;
  }
  accountInitials(account: AccountPermission): string {
    const parts = account.fullName.trim().split(/\s+/).filter(Boolean);
    return (parts.length > 1 ? `${parts[0][0]}${parts.at(-1)?.[0]}` : account.username.slice(0, 2)).toUpperCase();
  }

  openRoleEditor(role: RolePermission): void {
    if (this.saving()) return;
    this.editingRoleId.set(role.id);
    this.roleEditorPermissionIds.set(new Set(role.permissionIds));
    this.roleEditorDirty.set(false);
    this.roleEditorOpen.set(true);
  }

  closeRoleEditor(): void {
    if (this.saving()) return;
    if (this.roleEditorDirty() && !confirm('Bạn có thay đổi quyền nhóm chưa lưu. Đóng và bỏ thay đổi?')) return;
    this.roleEditorOpen.set(false);
    this.editingRoleId.set(null);
  }

  toggleRolePermission(permissionId: number): void {
    const role = this.editingRole();
    if (!role || role.isSystemAdmin || this.saving()) return;
    this.roleEditorPermissionIds.set(this.toggleWithDependencies(this.roleEditorPermissionIds(), permissionId));
    this.roleEditorDirty.set(true);
  }
  isRolePermissionGranted(permissionId: number): boolean { return this.roleEditorPermissionIds().has(permissionId); }
  roleGroupGrantedCount(group: PermissionGroup): number {
    return group.permissions.filter(permission => this.isRolePermissionGranted(permission.id)).length;
  }
  selectAllRolePermissions(): void {
    if (this.saving() || this.editingRole()?.isSystemAdmin) return;
    this.roleEditorPermissionIds.set(new Set((this.data()?.permissions ?? []).map(x => x.id)));
    this.roleEditorDirty.set(true);
  }
  clearRolePermissions(): void {
    if (this.saving() || this.editingRole()?.isSystemAdmin) return;
    this.roleEditorPermissionIds.set(new Set<number>());
    this.roleEditorDirty.set(true);
  }

  saveRole(): void {
    const role = this.editingRole();
    if (!role || role.isSystemAdmin || !this.roleEditorDirty() || this.saving()) return;
    this.saving.set(true);
    this.error.set('');
    this.api.updateRole(role.id, [...this.roleEditorPermissionIds()].sort((a, b) => a - b))
      .pipe(finalize(() => this.saving.set(false))).subscribe({
        next: updated => {
          this.applyUpdatedRole(updated);
          this.roleEditorPermissionIds.set(new Set(updated.permissionIds));
          this.roleEditorDirty.set(false);
          this.roleEditorOpen.set(false);
          this.editingRoleId.set(null);
          this.notice.set(`Đã cập nhật bộ quyền của nhóm ${updated.name}.`);
        },
        error: error => this.error.set(this.readError(error)),
      });
  }

  private applyAccount(account: AccountPermission): void {
    this.selectedAccountId.set(account.id);
    this.selectedAccountRoleIds.set(new Set(account.roleIds));
    this.selectedAccountPermissionIds.set(new Set(account.permissionIds));
    this.accountUsesCustomPermissions.set(account.usesCustomPermissions);
    this.accountDirty.set(false);
  }

  private refreshAccountDirty(): void {
    const original = this.selectedAccount();
    if (!original) {
      this.accountDirty.set(false);
      return;
    }
    const sameRoles = this.setEquals(this.selectedAccountRoleIds(), new Set(original.roleIds));
    const samePermissions = this.setEquals(this.selectedAccountPermissionIds(), new Set(original.permissionIds));
    this.accountDirty.set(!sameRoles || !samePermissions || this.accountUsesCustomPermissions() !== original.usesCustomPermissions);
  }

  private setEquals(left: ReadonlySet<number>, right: ReadonlySet<number>): boolean {
    return left.size === right.size && [...left].every(value => right.has(value));
  }

  private applyUpdatedRole(updatedRole: RolePermission): void {
    const current = this.data();
    if (!current) return;
    const roles = current.roles.map(role => role.id === updatedRole.id ? updatedRole : role);
    const accounts = current.accounts.map(account => {
      if (account.usesCustomPermissions || !account.roleIds.includes(updatedRole.id)) return account;
      return { ...account, permissionIds: this.permissionsForRoles(account.roleIds, roles) };
    });
    this.data.set({ ...current, roles, accounts });
    if (!this.accountUsesCustomPermissions() && this.selectedAccountRoleIds().has(updatedRole.id)) {
      this.selectedAccountPermissionIds.set(new Set(this.permissionsForRoles(this.selectedAccountRoleIds(), roles)));
    }
  }

  private permissionsForRoles(roleIds: Iterable<number>, roles = this.roles()): number[] {
    const ids = new Set(roleIds);
    const assigned = roles.filter(role => ids.has(role.id));
    if (assigned.some(role => role.isSystemAdmin)) return (this.data()?.permissions ?? []).map(x => x.id).sort((a, b) => a - b);
    return [...new Set(assigned.flatMap(role => role.permissionIds))].sort((a, b) => a - b);
  }

  private toggleWithDependencies(current: ReadonlySet<number>, permissionId: number): Set<number> {
    const permissions = this.data()?.permissions ?? [];
    const permission = permissions.find(item => item.id === permissionId);
    const next = new Set(current);
    if (!permission) return next;
    if (next.has(permissionId)) {
      next.delete(permissionId);
      if (permission.action === 'VIEW') permissions.filter(item => item.feature === permission.feature).forEach(item => next.delete(item.id));
    } else {
      next.add(permissionId);
      if (permission.action !== 'VIEW') {
        const view = permissions.find(item => item.feature === permission.feature && item.action === 'VIEW');
        if (view) next.add(view.id);
      }
    }
    return next;
  }

  private featureLabel(feature: string): string {
    return ({ ORDER: 'Đơn đặt hàng', CUSTOMER: 'Khách hàng', PRODUCT: 'Hàng hóa', ACCOUNT: 'Tài khoản', PERMISSION: 'Phân quyền', OTHER: 'Khác' } as Record<string, string>)[feature] ?? feature;
  }
  private featureDescription(feature: string): string {
    return ({ ORDER: 'Xem và xử lý vòng đời đơn hàng', CUSTOMER: 'Tra cứu và cập nhật hồ sơ khách hàng', PRODUCT: 'Tra cứu hàng hóa, giá bán và tồn kho', ACCOUNT: 'Quản lý tài khoản đăng nhập', PERMISSION: 'Cấu hình quyền cho các nhóm người dùng', OTHER: 'Các chức năng khác trong hệ thống' } as Record<string, string>)[feature] ?? 'Quyền sử dụng chức năng';
  }
  private readError(error: unknown): string {
    return error instanceof HttpErrorResponse ? error.error?.message ?? 'Không thể xử lý yêu cầu phân quyền.' : 'Đã xảy ra lỗi. Vui lòng thử lại.';
  }
}
