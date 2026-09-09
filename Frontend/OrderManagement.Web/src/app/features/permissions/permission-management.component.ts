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
  styleUrl: './permission-management.component.scss',
})
export class PermissionManagementComponent {
  private readonly api = inject(PermissionApiService);

  readonly data = signal<PermissionManagement | null>(null);
  readonly viewMode = signal<'accounts' | 'roles'>('accounts');
  readonly selectedRoleId = signal<number | null>(null);
  readonly selectedPermissionIds = signal<ReadonlySet<number>>(new Set<number>());
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly dirty = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  readonly accountKeyword = signal('');
  readonly selectedAccountId = signal<number | null>(null);
  readonly selectedAccountRoleId = signal<number | null>(null);
  readonly selectedAccountPermissionIds = signal<ReadonlySet<number>>(new Set<number>());
  readonly accountUsesCustomPermissions = signal(false);
  readonly accountDirty = signal(false);

  readonly roles = computed(() => this.data()?.roles ?? []);
  readonly accounts = computed(() => this.data()?.accounts ?? []);
  readonly filteredAccounts = computed(() => {
    const keyword = this.accountKeyword().trim().toLocaleLowerCase('vi');
    if (!keyword) return this.accounts();
    return this.accounts().filter(account =>
      `${account.fullName} ${account.username} ${account.roleName}`.toLocaleLowerCase('vi').includes(keyword),
    );
  });
  readonly selectedRole = computed(() =>
    this.roles().find(role => role.id === this.selectedRoleId()) ?? null,
  );
  readonly selectedAccount = computed(() =>
    this.accounts().find(account => account.id === this.selectedAccountId()) ?? null,
  );
  readonly selectedAccountRole = computed(() =>
    this.roles().find(role => role.id === this.selectedAccountRoleId()) ?? null,
  );
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

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.api.getPermissions().pipe(finalize(() => this.loading.set(false))).subscribe({
      next: data => {
        this.data.set(data);
        const current = data.roles.find(role => role.id === this.selectedRoleId()) ?? data.roles[0];
        if (current) this.applyRole(current);
        const currentAccount = data.accounts.find(account => account.id === this.selectedAccountId()) ?? data.accounts[0];
        if (currentAccount) this.applyAccount(currentAccount);
      },
      error: error => this.error.set(this.readError(error)),
    });
  }

  setViewMode(mode: 'accounts' | 'roles'): void {
    if (mode === this.viewMode() || this.saving()) return;
    const hasChanges = this.viewMode() === 'accounts' ? this.accountDirty() : this.dirty();
    if (hasChanges && !confirm('Bạn có thay đổi chưa lưu. Chuyển chế độ và bỏ các thay đổi này?')) return;
    this.viewMode.set(mode);
    this.error.set('');
    this.notice.set('');
  }

  selectAccount(account: AccountPermission): void {
    if (this.saving() || account.id === this.selectedAccountId()) return;
    if (this.accountDirty() && !confirm('Bạn có thay đổi chưa lưu. Chuyển tài khoản và bỏ các thay đổi này?')) return;
    this.applyAccount(account);
    this.error.set('');
    this.notice.set('');
  }

  changeAccountRole(roleId: number): void {
    if (this.saving() || this.selectedAccountIsLastAdmin()) return;
    const role = this.roles().find(item => item.id === roleId);
    if (!role) return;
    this.selectedAccountRoleId.set(role.id);
    this.selectedAccountPermissionIds.set(new Set(role.permissionIds));
    this.accountUsesCustomPermissions.set(false);
    this.accountDirty.set(true);
    this.notice.set('');
  }

  toggleAccountPermission(permissionId: number): void {
    if (this.saving() || this.selectedAccountRole()?.isSystemAdmin) return;
    const next = new Set(this.selectedAccountPermissionIds());
    next.has(permissionId) ? next.delete(permissionId) : next.add(permissionId);
    this.selectedAccountPermissionIds.set(next);
    this.accountUsesCustomPermissions.set(true);
    this.accountDirty.set(true);
    this.notice.set('');
  }

  resetAccountToRole(): void {
    const role = this.selectedAccountRole();
    if (!role || role.isSystemAdmin || this.saving()) return;
    this.selectedAccountPermissionIds.set(new Set(role.permissionIds));
    this.accountUsesCustomPermissions.set(false);
    this.accountDirty.set(true);
    this.notice.set('');
  }

  selectAllAccountPermissions(): void {
    if (this.saving() || this.selectedAccountRole()?.isSystemAdmin) return;
    this.selectedAccountPermissionIds.set(new Set((this.data()?.permissions ?? []).map(x => x.id)));
    this.accountUsesCustomPermissions.set(true);
    this.accountDirty.set(true);
    this.notice.set('');
  }

  clearAccountPermissions(): void {
    if (this.saving() || this.selectedAccountRole()?.isSystemAdmin) return;
    this.selectedAccountPermissionIds.set(new Set<number>());
    this.accountUsesCustomPermissions.set(true);
    this.accountDirty.set(true);
    this.notice.set('');
  }

  saveAccount(): void {
    const account = this.selectedAccount();
    const role = this.selectedAccountRole();
    if (!account || !role || !this.accountDirty() || this.saving()) return;

    this.saving.set(true);
    this.error.set('');
    const permissionIds = [...this.selectedAccountPermissionIds()].sort((a, b) => a - b);
    this.api.updateAccount(
      account.id,
      role.id,
      this.accountUsesCustomPermissions(),
      permissionIds,
    ).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: updatedAccount => {
        const current = this.data();
        if (current) {
          this.data.set({
            ...current,
            accounts: current.accounts.map(item => item.id === updatedAccount.id ? updatedAccount : item),
          });
        }
        this.applyAccount(updatedAccount);
        this.notice.set(`Đã cập nhật quyền cho tài khoản ${updatedAccount.username}.`);
      },
      error: error => this.error.set(this.readError(error)),
    });
  }

  selectRole(role: RolePermission): void {
    if (this.saving() || role.id === this.selectedRoleId()) return;
    if (this.dirty() && !confirm('Bạn có thay đổi chưa lưu. Chuyển nhóm và bỏ các thay đổi này?')) return;
    this.applyRole(role);
    this.error.set('');
    this.notice.set('');
  }

  togglePermission(permissionId: number): void {
    if (this.saving() || this.selectedRole()?.isSystemAdmin) return;
    const next = new Set(this.selectedPermissionIds());
    next.has(permissionId) ? next.delete(permissionId) : next.add(permissionId);
    this.selectedPermissionIds.set(next);
    this.dirty.set(true);
    this.notice.set('');
  }

  selectAll(): void {
    if (this.saving() || this.selectedRole()?.isSystemAdmin) return;
    this.selectedPermissionIds.set(new Set((this.data()?.permissions ?? []).map(x => x.id)));
    this.dirty.set(true);
    this.notice.set('');
  }

  clearAll(): void {
    if (this.saving() || this.selectedRole()?.isSystemAdmin) return;
    this.selectedPermissionIds.set(new Set<number>());
    this.dirty.set(true);
    this.notice.set('');
  }

  save(): void {
    const role = this.selectedRole();
    if (!role || role.isSystemAdmin || !this.dirty()) return;

    this.saving.set(true);
    this.error.set('');
    const permissionIds = [...this.selectedPermissionIds()].sort((a, b) => a - b);
    this.api.updateRole(role.id, permissionIds).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: updatedRole => {
        const current = this.data();
        if (current) {
          this.data.set({
            ...current,
            roles: current.roles.map(item => item.id === updatedRole.id ? updatedRole : item),
            accounts: current.accounts.map(account =>
              !account.usesCustomPermissions && account.roleId === updatedRole.id
                ? { ...account, permissionIds: updatedRole.permissionIds }
                : account,
            ),
          });
        }
        this.applyRole(updatedRole);
        this.notice.set(`Đã cập nhật quyền cho nhóm ${updatedRole.name}.`);
      },
      error: error => this.error.set(this.readError(error)),
    });
  }

  isGranted(permissionId: number): boolean {
    return this.selectedPermissionIds().has(permissionId);
  }

  isAccountPermissionGranted(permissionId: number): boolean {
    return this.selectedAccountPermissionIds().has(permissionId);
  }

  grantedCount(role: RolePermission): number {
    return role.isSystemAdmin ? (this.data()?.permissions.length ?? 0) : role.permissionIds.length;
  }

  groupGrantedCount(group: PermissionGroup): number {
    return group.permissions.filter(permission => this.isGranted(permission.id)).length;
  }

  accountGroupGrantedCount(group: PermissionGroup): number {
    return group.permissions.filter(permission => this.isAccountPermissionGranted(permission.id)).length;
  }

  accountInitials(account: AccountPermission): string {
    const parts = account.fullName.trim().split(/\s+/).filter(Boolean);
    return (parts.length > 1 ? `${parts[0][0]}${parts.at(-1)?.[0]}` : account.username.slice(0, 2)).toUpperCase();
  }

  private applyRole(role: RolePermission): void {
    this.selectedRoleId.set(role.id);
    this.selectedPermissionIds.set(new Set(role.permissionIds));
    this.dirty.set(false);
  }

  private applyAccount(account: AccountPermission): void {
    this.selectedAccountId.set(account.id);
    this.selectedAccountRoleId.set(account.roleId);
    this.selectedAccountPermissionIds.set(new Set(account.permissionIds));
    this.accountUsesCustomPermissions.set(account.usesCustomPermissions);
    this.accountDirty.set(false);
  }

  private featureLabel(feature: string): string {
    return ({
      ORDER: 'Đơn đặt hàng',
      CUSTOMER: 'Khách hàng',
      PRODUCT: 'Hàng hóa',
      ACCOUNT: 'Tài khoản',
      PERMISSION: 'Phân quyền',
      OTHER: 'Khác',
    } as Record<string, string>)[feature] ?? feature;
  }

  private featureDescription(feature: string): string {
    return ({
      ORDER: 'Xem và xử lý vòng đời đơn hàng',
      CUSTOMER: 'Tra cứu và cập nhật hồ sơ khách hàng',
      PRODUCT: 'Tra cứu hàng hóa, giá bán và tồn kho',
      ACCOUNT: 'Quản lý tài khoản đăng nhập',
      PERMISSION: 'Cấu hình quyền cho các nhóm người dùng',
      OTHER: 'Các chức năng khác trong hệ thống',
    } as Record<string, string>)[feature] ?? 'Quyền sử dụng chức năng';
  }

  private readError(error: unknown): string {
    return error instanceof HttpErrorResponse
      ? error.error?.message ?? 'Không thể xử lý yêu cầu phân quyền.'
      : 'Đã xảy ra lỗi. Vui lòng thử lại.';
  }
}
