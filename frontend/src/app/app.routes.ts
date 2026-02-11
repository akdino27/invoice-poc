import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: '',
    loadComponent: () => import('./shared/components/layout/layout.component').then(m => m.LayoutComponent),
    canActivate: [authGuard],
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'upload',
        loadComponent: () => import('./features/upload/upload-invoice.component').then(m => m.UploadInvoiceComponent)
      },
      {
        path: 'files',
        loadComponent: () => import('./features/files/files.component').then(m => m.FilesComponent)
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
