import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { OrderDetail, OrderListItem, OrderLookups, OrderSearchParams, PagedResult, SaveOrderRequest } from '../models/order.model';

@Injectable({ providedIn: 'root' })
export class OrderApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5000/api/orders';

  search(request: OrderSearchParams): Observable<PagedResult<OrderListItem>> {
    let params = new HttpParams().set('page', request.page).set('pageSize', request.pageSize);
    if (request.keyword) params = params.set('keyword', request.keyword);
    if (request.status) params = params.set('status', request.status);
    if (request.fromDate) params = params.set('fromDate', request.fromDate);
    if (request.toDate) params = params.set('toDate', request.toDate);
    if (request.creatorEmployeeId) params = params.set('creatorEmployeeId', request.creatorEmployeeId);
    if (request.deliveryEmployeeId) params = params.set('deliveryEmployeeId', request.deliveryEmployeeId);
    if (request.minTotal != null) params = params.set('minTotal', request.minTotal);
    if (request.maxTotal != null) params = params.set('maxTotal', request.maxTotal);
    if (request.sort) params = params.set('sort', request.sort);
    return this.http.get<PagedResult<OrderListItem>>(this.baseUrl, { params });
  }
  getById(id: number): Observable<OrderDetail> { return this.http.get<OrderDetail>(`${this.baseUrl}/${id}`); }
  getLookups(): Observable<OrderLookups> { return this.http.get<OrderLookups>(`${this.baseUrl}/lookups`); }
  create(request: SaveOrderRequest): Observable<OrderDetail> { return this.http.post<OrderDetail>(this.baseUrl, request); }
  update(id: number, request: SaveOrderRequest): Observable<OrderDetail> { return this.http.put<OrderDetail>(`${this.baseUrl}/${id}`, request); }
  delete(id: number): Observable<void> { return this.http.delete<void>(`${this.baseUrl}/${id}`); }
}
