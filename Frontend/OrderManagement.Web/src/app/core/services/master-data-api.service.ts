import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Customer, Product, SaveCustomer, SaveProduct } from '../models/master-data.model';

@Injectable({ providedIn: 'root' })
export class MasterDataApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5000/api';

  getCustomers(keyword = '') { return this.http.get<Customer[]>(`${this.apiUrl}/customers`, { params: keyword ? new HttpParams().set('keyword', keyword) : undefined }); }
  createCustomer(value: SaveCustomer) { return this.http.post<Customer>(`${this.apiUrl}/customers`, value); }
  updateCustomer(id: number, value: SaveCustomer) { return this.http.put<Customer>(`${this.apiUrl}/customers/${id}`, value); }
  deleteCustomer(id: number) { return this.http.delete<void>(`${this.apiUrl}/customers/${id}`); }
  getProducts(keyword = '') { return this.http.get<Product[]>(`${this.apiUrl}/products`, { params: keyword ? new HttpParams().set('keyword', keyword) : undefined }); }
  createProduct(value: SaveProduct) { return this.http.post<Product>(`${this.apiUrl}/products`, value); }
  updateProduct(id: number, value: SaveProduct) { return this.http.put<Product>(`${this.apiUrl}/products/${id}`, value); }
  deleteProduct(id: number) { return this.http.delete<void>(`${this.apiUrl}/products/${id}`); }
}
