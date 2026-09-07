import { Routes } from '@angular/router';
import { OrderManagementComponent } from './features/orders/order-management.component';
import { LoginComponent } from './features/login/login.component';
import { CustomerManagementComponent } from './features/customers/customer-management.component';
import { ProductManagementComponent } from './features/products/product-management.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'orders', component: OrderManagementComponent, canActivate: [authGuard] },
  { path: 'customers', component: CustomerManagementComponent, canActivate: [authGuard] },
  { path: 'products', component: ProductManagementComponent, canActivate: [authGuard] },
  { path: '', pathMatch: 'full', redirectTo: 'orders' },
  { path: '**', redirectTo: 'orders' },
];
