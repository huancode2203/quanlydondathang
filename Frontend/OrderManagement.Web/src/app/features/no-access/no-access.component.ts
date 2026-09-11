import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-no-access',
  standalone: true,
  template: `
    <section class="no-access-card">
      <span class="shield"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 3 4.5 6v5.2c0 4.7 3 8.2 7.5 9.8 4.5-1.6 7.5-5.1 7.5-9.8V6L12 3Z"/><path d="m9 9 6 6M15 9l-6 6"/></svg></span>
      <p>QUYỀN TRUY CẬP</p>
      <h2>Chưa có phân hệ được cấp quyền</h2>
      <span>Tài khoản <strong>{{ auth.user()?.username }}</strong> chưa có quyền xem phân hệ nào. Vui lòng liên hệ quản trị viên để được cấp quyền.</span>
      <button class="ui-button ui-button--primary" type="button" (click)="logout()">Đăng xuất</button>
    </section>
  `,
})
export class NoAccessComponent {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  logout(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}
