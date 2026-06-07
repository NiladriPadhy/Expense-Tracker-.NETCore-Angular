import { Routes } from '@angular/router';
import { LayoutComponent } from './presentation/core/layout.component';
import { adminGuard, authGuard, guestGuard } from './presentation/core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./presentation/features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./presentation/features/auth/register/register.component').then((m) => m.RegisterComponent),
  },
  {
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    children: [
      { path: '', pathMatch: 'full', loadComponent: () => import('./presentation/features/dashboard/dashboard.component').then((m) => m.DashboardComponent) },
      { path: 'profile', loadComponent: () => import('./presentation/features/profile.component').then((m) => m.ProfileComponent) },
      { path: 'months', loadComponent: () => import('./presentation/features/monthly-view/monthly-view.component').then((m) => m.MonthlyViewComponent) },
      { path: 'months/:y/:m', loadComponent: () => import('./presentation/features/monthly-view/monthly-view.component').then((m) => m.MonthlyViewComponent) },
      {
        path: 'admin',
        canActivate: [adminGuard],
        canActivateChild: [adminGuard],
        children: [
          { path: 'users', loadComponent: () => import('./presentation/features/admin/users/admin-users.component').then((m) => m.AdminUsersComponent) },
          { path: 'categories', loadComponent: () => import('./presentation/features/admin/categories/admin-categories.component').then((m) => m.AdminCategoriesComponent) },
          { path: 'currencies', loadComponent: () => import('./presentation/features/admin/currencies/admin-currencies.component').then((m) => m.AdminCurrenciesComponent) },
        ],
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
