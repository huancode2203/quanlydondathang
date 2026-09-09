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
      <button type="button" (click)="logout()">Đăng xuất</button>
    </section>
  `,
  styles: [`
    :host{display:grid;min-height:calc(100vh - 148px);place-items:center}.no-access-card{width:min(520px,100%);padding:42px 34px;text-align:center;background:#fff;border:1px solid #dce6ed;border-radius:18px;box-shadow:0 16px 42px rgba(16,42,67,.08)}.shield{width:62px;height:62px;margin:0 auto 18px;display:grid;place-items:center;border-radius:18px;background:#eef7f9;color:#087f98}.shield svg{width:32px;fill:none;stroke:currentColor;stroke-width:1.8;stroke-linecap:round;stroke-linejoin:round}.no-access-card p{margin:0 0 7px;color:#087f98;font-size:.75rem;font-weight:800;letter-spacing:.1em}.no-access-card h2{margin:0;color:#173b57;font-size:1.3rem}.no-access-card>span:not(.shield){display:block;margin-top:12px;color:#6f8798;font-size:.875rem;line-height:1.7}.no-access-card button{height:42px;margin-top:24px;padding:0 20px;border:0;border-radius:9px;background:#087f98;color:#fff;font-weight:800}@media(max-width:860px){:host{min-height:50vh}.no-access-card{padding:32px 22px}}
  `],
})
export class NoAccessComponent {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  logout(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}
