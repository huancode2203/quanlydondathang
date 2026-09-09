export interface Customer {
  id: number; code: string; name: string; phone?: string; email?: string;
  address?: string; taxCode?: string; note?: string;
}
export type SaveCustomer = Omit<Customer, 'id'>;

export interface Product {
  id: number; code: string; name: string; unit: string;
  price: number; orderedQuantity: number; availableQuantity: number;
  stockQuantity: number; description?: string;
}
export type SaveProduct = Omit<Product, 'id' | 'orderedQuantity' | 'availableQuantity'>;
