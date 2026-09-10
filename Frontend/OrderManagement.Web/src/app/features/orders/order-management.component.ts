import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { AbstractControl, ReactiveFormsModule, UntypedFormArray, UntypedFormBuilder, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { LookupItem, OrderDetail, OrderListItem, SaveOrderRequest } from '../../core/models/order.model';
import { OrderApiService } from '../../core/services/order-api.service';
import { AuthService } from '../../core/services/auth.service';
import { MasterDataApiService } from '../../core/services/master-data-api.service';
import { SaveCustomer } from '../../core/models/master-data.model';
import { PageToolbarComponent } from '../../shared/ui/page-toolbar.component';

@Component({
  selector: 'app-order-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageToolbarComponent],
  templateUrl: './order-management.component.html',
})
export class OrderManagementComponent implements OnInit {
  private readonly fb = inject(UntypedFormBuilder);
  private readonly api = inject(OrderApiService);
  private readonly masterApi = inject(MasterDataApiService);
  readonly auth = inject(AuthService);

  readonly orders = signal<OrderListItem[]>([]);
  readonly customers = signal<LookupItem[]>([]);
  readonly products = signal<LookupItem[]>([]);
  readonly employees = signal<LookupItem[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly formOpen = signal(false);
  readonly deleteTarget = signal<OrderListItem | null>(null);
  readonly errorMessage = signal('');
  readonly toastMessage = signal('');
  readonly page = signal(1);
  readonly total = signal(0);
  readonly totalPages = signal(1);
  readonly editingId = signal<number | null>(null);
  readonly advancedOpen = signal(false);
  readonly customerFormOpen = signal(false);
  readonly customerSaving = signal(false);
  readonly creatorName = signal('');

  readonly statuses = [
    { value: 'CHO_XAC_NHAN', label: 'Chờ xác nhận' },
    { value: 'DA_XAC_NHAN', label: 'Đã xác nhận' },
    { value: 'DANG_CHUAN_BI', label: 'Đang chuẩn bị' },
    { value: 'CHO_GIAO_HANG', label: 'Chờ giao hàng' },
    { value: 'DANG_GIAO', label: 'Đang giao' },
    { value: 'DA_GIAO', label: 'Đã giao' },
    { value: 'DA_HUY', label: 'Đã hủy' },
  ];

  readonly filterForm = this.fb.group({
    keyword: [''], status: [''], fromDate: [''], toDate: [''], creatorEmployeeId: [null],
    deliveryEmployeeId: [null], minTotal: [null], maxTotal: [null], sort: [''],
  });
  readonly orderForm = this.fb.group({
    code: ['', [Validators.required, Validators.maxLength(30)]],
    customerId: [null, Validators.required],
    deliveryEmployeeId: [null],
    orderedAt: ['', Validators.required],
    expectedDeliveryAt: ['', Validators.required],
    deliveredAt: [''],
    deliveryAddress: ['', [Validators.required, Validators.maxLength(500)]],
    status: ['CHO_XAC_NHAN', Validators.required],
    discountAmount: [0, [Validators.required, Validators.min(0)]],
    taxAmount: [0, [Validators.required, Validators.min(0)]],
    shippingFee: [0, [Validators.required, Validators.min(0)]],
    note: [''],
    items: this.fb.array([]),
  });
  readonly customerForm = this.fb.group({
    code: ['', Validators.required], name: ['', Validators.required], phone: [''], email: ['', Validators.email],
    address: [''], taxCode: [''], note: [''],
  });

  get items(): UntypedFormArray { return this.orderForm.get('items') as UntypedFormArray; }

  ngOnInit(): void {
    this.loadLookups();
    this.loadOrders();
  }

  loadOrders(): void {
    const filters = this.filterForm.getRawValue();
    this.loading.set(true);
    this.errorMessage.set('');
    this.api.search({
      keyword: filters.keyword?.trim() || undefined,
      status: filters.status || undefined,
      fromDate: filters.fromDate || undefined,
      toDate: filters.toDate || undefined,
      creatorEmployeeId: filters.creatorEmployeeId ? Number(filters.creatorEmployeeId) : undefined,
      deliveryEmployeeId: filters.deliveryEmployeeId ? Number(filters.deliveryEmployeeId) : undefined,
      minTotal: filters.minTotal != null && filters.minTotal !== '' ? Number(filters.minTotal) : undefined,
      maxTotal: filters.maxTotal != null && filters.maxTotal !== '' ? Number(filters.maxTotal) : undefined,
      sort: filters.sort || undefined,
      page: this.page(),
      pageSize: 10,
    }).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: result => {
        this.orders.set(result.items);
        this.total.set(result.total);
        this.totalPages.set(result.totalPages);
      },
      error: error => this.errorMessage.set(this.readError(error)),
    });
  }

  loadLookups(): void {
    this.api.getLookups().subscribe({
      next: data => {
        this.customers.set(data.customers);
        this.products.set(data.products);
        this.employees.set(data.employees);
      },
      error: error => this.errorMessage.set(this.readError(error)),
    });
  }

  search(): void {
    const value = this.filterForm.getRawValue();
    if (value.fromDate && value.toDate && value.fromDate > value.toDate) {
      this.showToast('Từ ngày không được lớn hơn đến ngày.');
      return;
    }
    if (Number(value.minTotal || 0) < 0 || Number(value.maxTotal || 0) < 0) {
      this.showToast('Khoảng tổng tiền không được âm.');
      return;
    }
    if (value.minTotal != null && value.minTotal !== '' && value.maxTotal != null && value.maxTotal !== '' && Number(value.minTotal) > Number(value.maxTotal)) {
      this.showToast('Tổng tiền nhỏ nhất không được lớn hơn tổng tiền lớn nhất.');
      return;
    }
    this.page.set(1);
    this.loadOrders();
  }

  resetFilters(): void {
    this.filterForm.reset({ keyword: '', status: '', fromDate: '', toDate: '', creatorEmployeeId: null, deliveryEmployeeId: null, minTotal: null, maxTotal: null, sort: '' });
    this.page.set(1);
    this.loadOrders();
  }

  changePage(nextPage: number): void {
    if (nextPage < 1 || nextPage > this.totalPages() || nextPage === this.page()) return;
    this.page.set(nextPage);
    this.loadOrders();
  }

  visiblePages(): number[] {
    const total = this.totalPages();
    const visibleCount = Math.min(5, total);
    let start = Math.max(1, this.page() - Math.floor(visibleCount / 2));
    const end = Math.min(total, start + visibleCount - 1);
    start = Math.max(1, end - visibleCount + 1);
    return Array.from({ length: end - start + 1 }, (_, index) => start + index);
  }

  openCreate(): void {
    this.editingId.set(null);
    this.creatorName.set(this.auth.user()?.fullName ?? '');
    const tomorrow = new Date(Date.now() + 24 * 60 * 60 * 1000);
    this.orderForm.reset({
      code: `DH${new Date().getFullYear()}${String(Date.now()).slice(-5)}`,
      orderedAt: this.toLocalInput(new Date().toISOString()),
      expectedDeliveryAt: this.toLocalInput(tomorrow.toISOString()),
      status: 'CHO_XAC_NHAN',
      discountAmount: 0,
      taxAmount: 0,
      shippingFee: 30000,
    });
    this.items.clear();
    this.addItem();
    this.formOpen.set(true);
  }

  openEdit(order: OrderListItem): void {
    this.errorMessage.set('');
    this.api.getById(order.id).subscribe({
      next: detail => this.fillForm(detail),
      error: error => this.showToast(this.readError(error)),
    });
  }

  closeForm(): void {
    if (!this.saving()) this.formOpen.set(false);
  }

  addItem(item?: Partial<{ productId: number; quantity: number; unitPrice: number; discountPercent: number; note: string }>): void {
    this.items.push(this.fb.group({
      productId: [item?.productId ?? null, Validators.required],
      quantity: [item?.quantity ?? 1, [Validators.required, Validators.min(0.01)]],
      unitPrice: [item?.unitPrice ?? 0, [Validators.required, Validators.min(0)]],
      discountPercent: [item?.discountPercent ?? 0, [Validators.required, Validators.min(0), Validators.max(100)]],
      note: [item?.note ?? ''],
    }));
  }

  removeItem(index: number): void {
    if (this.items.length > 1) this.items.removeAt(index);
  }

  productChanged(group: AbstractControl, index: number): void {
    const selectedId = Number(group.get('productId')?.value);
    if (selectedId && this.isProductUsed(selectedId, index)) {
      group.patchValue({ productId: null, unitPrice: 0 });
      this.showToast('Hàng hóa này đã có trong đơn, không thể thêm lại.');
      return;
    }
    const product = this.products().find(x => x.id === selectedId);
    if (product) group.patchValue({ unitPrice: product.price ?? 0 });
  }

  lineTotal(group: AbstractControl): number {
    const value = group.getRawValue();
    return Number(value.quantity || 0) * Number(value.unitPrice || 0) * (1 - Number(value.discountPercent || 0) / 100);
  }

  merchandiseTotal(): number {
    return this.items.controls.reduce((sum, control) => sum + this.lineTotal(control), 0);
  }

  grandTotal(): number {
    const value = this.orderForm.getRawValue();
    return this.merchandiseTotal() - Number(value.discountAmount || 0) + Number(value.taxAmount || 0) + Number(value.shippingFee || 0);
  }

  isProductUsed(productId: number, currentIndex: number): boolean {
    return this.items.controls.some((control, index) => index !== currentIndex && Number(control.get('productId')?.value) === productId);
  }

  saveOrder(): void {
    if (this.orderForm.invalid || this.items.length === 0) {
      this.orderForm.markAllAsTouched();
      this.showToast('Vui lòng nhập đầy đủ các trường bắt buộc.');
      return;
    }
    const productIds = this.items.controls.map(x => Number(x.get('productId')?.value));
    if (new Set(productIds).size !== productIds.length) {
      this.showToast('Một hàng hóa không được chọn nhiều lần trong cùng đơn hàng.');
      return;
    }
    const value = this.orderForm.getRawValue();
    const now = new Date();
    const orderedAt = new Date(value.orderedAt);
    const expectedAt = new Date(value.expectedDeliveryAt);
    if (orderedAt > now || orderedAt < new Date(new Date().setFullYear(now.getFullYear() - 10))) {
      this.showToast('Ngày đặt hàng phải từ 10 năm trước đến thời điểm hiện tại.'); return;
    }
    if (expectedAt <= now || expectedAt <= orderedAt) {
      this.showToast('Ngày và giờ giao dự kiến phải sau hiện tại và sau ngày đặt hàng.'); return;
    }
    if (this.grandTotal() < 0) {
      this.showToast('Tổng tiền đang âm. Vui lòng giảm số tiền chiết khấu.'); return;
    }

    const request = this.buildRequest();
    const id = this.editingId();
    const operation = id ? this.api.update(id, request) : this.api.create(request);
    this.saving.set(true);
    operation.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => {
        this.formOpen.set(false);
        this.showToast(id ? 'Đã cập nhật đơn hàng.' : 'Đã tạo đơn hàng mới.');
        this.loadOrders();
      },
      error: error => this.showToast(this.readError(error)),
    });
  }

  askDelete(order: OrderListItem): void { this.deleteTarget.set(order); }
  cancelDelete(): void { this.deleteTarget.set(null); }

  confirmDelete(): void {
    const order = this.deleteTarget();
    if (!order) return;
    this.api.delete(order.id).subscribe({
      next: () => {
        this.deleteTarget.set(null);
        this.showToast(`Đã xóa đơn hàng ${order.code}.`);
        this.loadOrders();
      },
      error: error => this.showToast(this.readError(error)),
    });
  }

  statusLabel(status: string): string { return this.statuses.find(x => x.value === status)?.label ?? status; }
  statusClass(status: string): string { return status.toLowerCase().replaceAll('_', '-'); }
  minOrderDate(): string { const date = new Date(); date.setFullYear(date.getFullYear() - 10); return this.toLocalInput(date.toISOString()); }
  maxOrderDate(): string { return this.toLocalInput(new Date().toISOString()); }
  minDeliveryDate(): string { return this.toLocalInput(new Date(Date.now() + 60000).toISOString()); }

  openCustomerForm(): void {
    this.customerForm.reset({ code: `KH${String(Date.now()).slice(-5)}` });
    this.customerFormOpen.set(true);
  }

  createCustomer(): void {
    if (this.customerForm.invalid) { this.customerForm.markAllAsTouched(); return; }
    this.customerSaving.set(true);
    this.masterApi.createCustomer(this.customerForm.getRawValue() as SaveCustomer).pipe(
      finalize(() => this.customerSaving.set(false)),
    ).subscribe({
      next: customer => {
        const lookup: LookupItem = { id: customer.id, code: customer.code, name: customer.name, extra: customer.phone };
        this.customers.update(values => [...values, lookup].sort((a, b) => a.name.localeCompare(b.name, 'vi')));
        this.orderForm.patchValue({ customerId: customer.id, deliveryAddress: customer.address ?? '' });
        this.customerFormOpen.set(false);
        this.showToast('Đã tạo và chọn khách hàng mới.');
      },
      error: error => this.showToast(this.readError(error)),
    });
  }

  private fillForm(order: OrderDetail): void {
    this.editingId.set(order.id);
    this.creatorName.set(order.creatorName);
    this.orderForm.reset({
      code: order.code,
      customerId: order.customerId,
      deliveryEmployeeId: order.deliveryEmployeeId ?? null,
      orderedAt: this.toLocalInput(order.orderedAt),
      expectedDeliveryAt: this.toLocalInput(order.expectedDeliveryAt),
      deliveredAt: this.toLocalInput(order.deliveredAt),
      deliveryAddress: order.deliveryAddress,
      status: order.status,
      discountAmount: order.discountAmount,
      taxAmount: order.taxAmount,
      shippingFee: order.shippingFee,
      note: order.note ?? '',
    });
    this.items.clear();
    order.items.forEach(item => this.addItem(item));
    this.formOpen.set(true);
  }

  private buildRequest(): SaveOrderRequest {
    const value = this.orderForm.getRawValue();
    return {
      code: value.code.trim(),
      customerId: Number(value.customerId),
      deliveryEmployeeId: value.deliveryEmployeeId ? Number(value.deliveryEmployeeId) : undefined,
      // datetime-local represents business time at the SQL Server location.
      // Keep it timezone-free so the API does not shift the delivery hour to UTC.
      orderedAt: value.orderedAt,
      expectedDeliveryAt: this.toApiDateTime(value.expectedDeliveryAt),
      deliveredAt: this.toApiDateTime(value.deliveredAt),
      deliveryAddress: value.deliveryAddress.trim(),
      status: value.status,
      discountAmount: Number(value.discountAmount || 0),
      taxAmount: Number(value.taxAmount || 0),
      shippingFee: Number(value.shippingFee || 0),
      note: value.note?.trim() || undefined,
      items: value.items.map((item: Record<string, unknown>) => ({
        productId: Number(item['productId']),
        quantity: Number(item['quantity']),
        unitPrice: Number(item['unitPrice']),
        discountPercent: Number(item['discountPercent']),
        note: String(item['note'] ?? '').trim() || undefined,
      })),
    };
  }

  private toApiDateTime(value?: string): string | undefined { return value || undefined; }
  private toLocalInput(value?: string): string { if (!value) return ''; const date = new Date(value); const offset = date.getTimezoneOffset(); return new Date(date.getTime() - offset * 60000).toISOString().slice(0, 16); }
  private readError(error: unknown): string { return error instanceof HttpErrorResponse ? error.error?.message || 'Không thể kết nối đến API.' : 'Đã xảy ra lỗi không xác định.'; }
  private showToast(message: string): void { this.toastMessage.set(message); window.setTimeout(() => this.toastMessage.set(''), 3200); }
}
