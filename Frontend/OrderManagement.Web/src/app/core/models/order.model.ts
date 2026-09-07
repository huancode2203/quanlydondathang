export interface LookupItem { id: number; code: string; name: string; extra?: string; price?: number; stockQuantity?: number; }
export interface OrderLookups { customers: LookupItem[]; products: LookupItem[]; employees: LookupItem[]; }
export interface OrderListItem {
  id: number; code: string; customerId: number; customerName: string; customerPhone?: string;
  creatorName: string; deliveryEmployeeName?: string; orderedAt: string; expectedDeliveryAt?: string;
  deliveryAddress: string; grandTotal: number; status: string; itemCount: number;
}
export interface OrderItem {
  id: number; productId: number; productCode: string; productName: string; unit: string;
  quantity: number; unitPrice: number; discountPercent: number; lineTotal: number; stockQuantity: number; note?: string;
}
export interface OrderDetail extends OrderListItem {
  creatorEmployeeId: number; deliveryEmployeeId?: number; deliveredAt?: string; merchandiseTotal: number;
  discountAmount: number; taxAmount: number; shippingFee: number; note?: string; items: OrderItem[];
}
export interface SaveOrderItem { productId: number; quantity: number; unitPrice: number; discountPercent: number; note?: string; }
export interface SaveOrderRequest {
  code: string; customerId: number; deliveryEmployeeId?: number;
  orderedAt: string; expectedDeliveryAt?: string; deliveredAt?: string; deliveryAddress: string;
  discountAmount: number; taxAmount: number; shippingFee: number; status: string; note?: string; items: SaveOrderItem[];
}
export interface PagedResult<T> { items: T[]; total: number; page: number; pageSize: number; totalPages: number; }
export interface OrderSearchParams {
  keyword?: string; status?: string; fromDate?: string; toDate?: string;
  creatorEmployeeId?: number; deliveryEmployeeId?: number; minTotal?: number; maxTotal?: number;
  sort?: string; page: number; pageSize: number;
}
