import { Routes } from '@angular/router';

export const adminRoutes: Routes = [
  {
    path: 'analytics',
    loadComponent: () => import('./analytics.component').then((m) => m.AnalyticsComponent),
  },
  {
    path: 'logs',
    loadComponent: () => import('./logs.component').then((m) => m.LogsComponent),
  },
  { path: '', redirectTo: 'analytics', pathMatch: 'full' },
];
