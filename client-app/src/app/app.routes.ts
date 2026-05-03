import { Routes } from '@angular/router';
import { authGuard, adminGuard } from './shared/utils/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'products', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () => import('./login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'products',
    loadComponent: () => import('./products/products.component').then((m) => m.ProductsComponent),
    canActivate: [authGuard],
  },
  {
    path: 'checkout/:productId',
    loadComponent: () => import('./checkout/checkout.component').then((m) => m.CheckoutComponent),
    canActivate: [authGuard],
  },
  {
    path: 'order-placed',
    loadComponent: () =>
      import('./order-placed/order-placed.component').then((m) => m.OrderPlacedComponent),
    canActivate: [authGuard],
  },
  {
    path: 'orders',
    loadComponent: () =>
      import('./order-status/order-status.component').then((m) => m.OrderStatusComponent),
    canActivate: [authGuard],
  },
  // Admin routes bundled into a single lazy chunk; canMatch blocks chunk download for non-admins
  {
    path: 'admin',
    canMatch: [adminGuard],
    loadChildren: () => import('./admin/admin.routes').then((m) => m.adminRoutes),
  },
  { path: '**', redirectTo: 'products' },
];
