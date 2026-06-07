import { HttpErrorResponse, HttpEvent, HttpHandlerFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, switchMap, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthRepository } from '../../domain/ports/auth.repository';
import { API_CONFIG } from '../config/api.config';
import { JwtTokenStore } from './jwt-token.store';

let refreshInFlight: Observable<string> | null = null;

export const authInterceptor = (req: HttpRequest<unknown>, next: HttpHandlerFn): Observable<HttpEvent<unknown>> => {
  const store = inject(JwtTokenStore);
  const auth = inject(AuthRepository);
  const router = inject(Router);
  const cfg = inject(API_CONFIG);

  const isApi = req.url.startsWith(cfg.baseUrl) || req.url.startsWith('/api/');
  const isAuthEndpoint = req.url.includes('/auth/login') || req.url.includes('/auth/register') || req.url.includes('/auth/refresh');

  const token = store.accessToken();
  const withAuth = isApi && token && !isAuthEndpoint
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(withAuth).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status !== 401 || isAuthEndpoint || !store.refreshToken()) {
        return throwError(() => err);
      }
      const refreshToken = store.refreshToken()!;
      if (!refreshInFlight) {
        refreshInFlight = new Observable<string>((sub) => {
          auth.refresh(refreshToken).subscribe({
            next: (r) => {
              store.setSession(r);
              sub.next(r.accessToken);
              sub.complete();
              refreshInFlight = null;
            },
            error: (e) => {
              store.clear();
              router.navigateByUrl('/login');
              sub.error(e);
              refreshInFlight = null;
            },
          });
        });
      }
      return refreshInFlight.pipe(
        switchMap((newToken) => next(req.clone({ setHeaders: { Authorization: `Bearer ${newToken}` } })))
      );
    })
  );
};
