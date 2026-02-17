import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth-guard';
import { guestGuard } from './core/guards/guest-guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/landing/landing')
        .then(m => m.Landing),
  },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/login/login')
        .then(m => m.Login),
  },
  {
    path: 'signup',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/signup/signup')
        .then(m => m.Signup),
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/dashboard/dashboard')
        .then(m => m.Dashboard),
  },
  {
    path: 'upload',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/upload/upload')
        .then(m => m.Upload),
  },
  {
    path: 'invoices',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/invoices/invoice-list/invoice-list')
        .then(m => m.InvoiceList),
  },
  {
    path: 'invoices/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/invoices/invoice-detail/invoice-detail')
        .then(m => m.InvoiceDetail),
  },
  {
    path: 'products',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/products/product-list/product-list')
        .then(m => m.ProductList),
  },
  {
    path: 'admin',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/admin/admin')
        .then(m => m.Admin),
  },
  {
    path: '**',
    redirectTo: ''
  }
];
