import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { Permission, PermissionManagement, RolePermission } from '../../core/models/permission.model';
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
  readonly selectedRoleId = signal<number | null>(null);
  readonly selectedPermissionIds = signal<ReadonlySet<number>>(new Set<number>());
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly dirty = signal(false);
  readonly error = signal('');
  readonly notice = signal('');

  readonly roles = computed(() => this.data()?.roles ?? []);
  readonly selectedRole = computed(() =>
    this.roles().find(role => role.id === this.selectedRoleId()) ?? null,
  );
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

  grantedCount(role: RolePermission): number {
    return role.isSystemAdmin ? (this.data()?.permissions.length ?? 0) : role.permissionIds.length;
  }

  groupGrantedCount(group: PermissionGroup): number {
    return group.permissions.filter(permission => this.isGranted(permission.id)).length;
  }

  private applyRole(role: RolePermission): void {
    this.selectedRoleId.set(role.id);
    this.selectedPermissionIds.set(new Set(role.permissionIds));
    this.dirty.set(false);
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
