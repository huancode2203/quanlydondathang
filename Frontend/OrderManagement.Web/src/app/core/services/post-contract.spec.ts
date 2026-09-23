import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Observable } from 'rxjs';
import { SaveCustomer, SaveProduct } from '../models/master-data.model';
import { SaveOrderRequest } from '../models/order.model';
import { AuthService, SESSION_KEY } from './auth.service';
import { MasterDataApiService } from './master-data-api.service';
import { OrderApiService } from './order-api.service';
import { PermissionApiService } from './permission-api.service';

const customer: SaveCustomer = { code: 'KH01', name: 'Khách hàng' };
const product: SaveProduct = { code: 'HH01', name: 'Hàng hóa', unit: 'Cái', price: 20, stockQuantity: 5 };
const order: SaveOrderRequest = {
  code: 'DH01', customerId: 1, orderedAt: '2026-09-22T09:00:00',
  expectedDeliveryAt: '2026-09-23T09:00:00', deliveryAddress: 'TP.HCM',
  discountAmount: 0, taxAmount: 0, shippingFee: 0, status: 'CHO_XAC_NHAN',
  items: [{ productId: 1, quantity: 1, unitPrice: 20, discountPercent: 0 }],
};

describe('Business APIs use POST-only JSON contracts', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.removeItem(SESSION_KEY);
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.removeItem(SESSION_KEY);
  });

  const cases: { path: string; body: object; invoke: () => Observable<unknown> }[] = [
    { path: 'orders/search', body: { page: 2, pageSize: 10, keyword: 'KH' }, invoke: () => TestBed.inject(OrderApiService).search({ page: 2, pageSize: 10, keyword: 'KH' }) },
    { path: 'orders/detail', body: { id: 7 }, invoke: () => TestBed.inject(OrderApiService).getById(7) },
    { path: 'orders/lookups', body: {}, invoke: () => TestBed.inject(OrderApiService).getLookups() },
    { path: 'orders/create', body: order, invoke: () => TestBed.inject(OrderApiService).create(order) },
    { path: 'orders/update', body: { ...order, id: 7 }, invoke: () => TestBed.inject(OrderApiService).update(7, order) },
    { path: 'orders/delete', body: { id: 7 }, invoke: () => TestBed.inject(OrderApiService).delete(7) },
    { path: 'customers/search', body: { keyword: 'KH' }, invoke: () => TestBed.inject(MasterDataApiService).getCustomers('KH') },
    { path: 'customers/create', body: customer, invoke: () => TestBed.inject(MasterDataApiService).createCustomer(customer) },
    { path: 'customers/update', body: { ...customer, id: 2 }, invoke: () => TestBed.inject(MasterDataApiService).updateCustomer(2, customer) },
    { path: 'customers/delete', body: { id: 2 }, invoke: () => TestBed.inject(MasterDataApiService).deleteCustomer(2) },
    { path: 'products/search', body: { keyword: 'HH' }, invoke: () => TestBed.inject(MasterDataApiService).getProducts('HH') },
    { path: 'products/create', body: product, invoke: () => TestBed.inject(MasterDataApiService).createProduct(product) },
    { path: 'products/update', body: { ...product, id: 3 }, invoke: () => TestBed.inject(MasterDataApiService).updateProduct(3, product) },
    { path: 'products/delete', body: { id: 3 }, invoke: () => TestBed.inject(MasterDataApiService).deleteProduct(3) },
    { path: 'permissions/search', body: {}, invoke: () => TestBed.inject(PermissionApiService).getPermissions() },
    { path: 'permissions/roles/update', body: { id: 2, permissionIds: [1, 2] }, invoke: () => TestBed.inject(PermissionApiService).updateRole(2, [1, 2]) },
    { path: 'permissions/accounts/update', body: { id: 5, roleIds: [2, 3], usesCustomPermissions: false, permissionIds: [] }, invoke: () => TestBed.inject(PermissionApiService).updateAccount(5, [2, 3], false, []) },
  ];

  for (const test of cases) {
    it(test.path, () => {
      test.invoke().subscribe();
      const request = http.expectOne(`/api/${test.path}`);
      expect(request.request.method).toBe('POST');
      expect(request.request.body).toEqual(test.body);
      expect(request.request.params.keys()).toEqual([]);
      request.flush({ status: 200, value: null, message: '' });
    });
  }

  it('auth/login saves only the unwrapped session', () => {
    const session = {
      token: 'test-token', expiresAt: new Date(Date.now() + 60_000).toISOString(),
      employeeId: 1, username: 'test', fullName: 'Test user', roleName: 'Sales', permissions: ['ORDER_VIEW'],
    };
    const auth = TestBed.inject(AuthService);
    auth.login('test', 'password').subscribe();
    const request = http.expectOne('/api/auth/login');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ username: 'test', password: 'password' });
    request.flush({ status: 200, value: session, message: '' });
    expect(auth.user()).toEqual(session);
    expect(JSON.parse(localStorage.getItem(SESSION_KEY)!)).toEqual(session);
  });
});
