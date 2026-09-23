import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { OrderDetail, OrderListItem, OrderLookups, OrderSearchParams, PagedResult, SaveOrderRequest } from '../models/order.model';
import { ApiClient } from './api-client.service';

@Injectable({ providedIn: 'root' })
export class OrderApiService {
  private readonly api = inject(ApiClient);

  search(request: OrderSearchParams): Observable<PagedResult<OrderListItem>> {
    return this.api.post('orders/search', request);
  }
  getById(id: number): Observable<OrderDetail> { return this.api.post('orders/detail', { id }); }
  getLookups(): Observable<OrderLookups> { return this.api.post('orders/lookups'); }
  create(request: SaveOrderRequest): Observable<OrderDetail> { return this.api.post('orders/create', request); }
  update(id: number, request: SaveOrderRequest): Observable<OrderDetail> { return this.api.post('orders/update', { ...request, id }); }
  delete(id: number): Observable<void> { return this.api.post('orders/delete', { id }); }
}
