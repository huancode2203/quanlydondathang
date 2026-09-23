import { inject, Injectable } from '@angular/core';
import { Customer, Product, SaveCustomer, SaveProduct } from '../models/master-data.model';
import { ApiClient } from './api-client.service';

@Injectable({ providedIn: 'root' })
export class MasterDataApiService {
  private readonly api = inject(ApiClient);

  getCustomers(keyword = '') { return this.api.post<Customer[]>('customers/search', { keyword }); }
  createCustomer(value: SaveCustomer) { return this.api.post<Customer>('customers/create', value); }
  updateCustomer(id: number, value: SaveCustomer) { return this.api.post<Customer>('customers/update', { ...value, id }); }
  deleteCustomer(id: number) { return this.api.post<void>('customers/delete', { id }); }
  getProducts(keyword = '') { return this.api.post<Product[]>('products/search', { keyword }); }
  createProduct(value: SaveProduct) { return this.api.post<Product>('products/create', value); }
  updateProduct(id: number, value: SaveProduct) { return this.api.post<Product>('products/update', { ...value, id }); }
  deleteProduct(id: number) { return this.api.post<void>('products/delete', { id }); }
}
