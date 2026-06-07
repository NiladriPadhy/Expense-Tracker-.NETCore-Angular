import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { JwtTokenStore } from '../../../infrastructure/auth/jwt-token.store';

export const authGuard: CanActivateFn = () => {
  const store = inject(JwtTokenStore);
  const router = inject(Router);
  if (store.isAuthenticated()) return true;
  return router.parseUrl('/login');
};

export const adminGuard: CanActivateFn = () => {
  const store = inject(JwtTokenStore);
  const router = inject(Router);
  if (store.isAuthenticated() && store.user()?.role === 'Admin') return true;
  return router.parseUrl('/');
};

export const guestGuard: CanActivateFn = () => {
  const store = inject(JwtTokenStore);
  const router = inject(Router);
  if (!store.isAuthenticated()) return true;
  return router.parseUrl('/');
};
