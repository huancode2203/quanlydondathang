import { Routes } from '@angular/router';
import { OrderManagementComponent } from './features/orders/order-management.component';
import { LoginComponent } from './features/login/login.component';
import { CustomerManagementComponent } from './features/customers/customer-management.component';
import { ProductManagementComponent } from './features/products/product-management.component';
import { PermissionManagementComponent } from './features/permissions/permission-management.component';
import { NoAccessComponent } from './features/no-access/no-access.component';
import { authGuard } from './core/guards/auth.guard';
import { permissionGuard, permissionManagementGuard } from './core/guards/permission.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'orders', component: OrderManagementComponent, canActivate: [authGuard, permissionGuard('ORDER_VIEW')] },
  { path: 'customers', component: CustomerManagementComponent, canActivate: [authGuard, permissionGuard('CUSTOMER_VIEW')] },
  { path: 'products', component: ProductManagementComponent, canActivate: [authGuard, permissionGuard('PRODUCT_VIEW')] },
  { path: 'permissions', component: PermissionManagementComponent, canActivate: [authGuard, permissionManagementGuard] },
  { path: 'no-access', component: NoAccessComponent, canActivate: [authGuard] },
  { path: '', pathMatch: 'full', redirectTo: 'orders' },
  { path: '**', redirectTo: 'orders' },
];
