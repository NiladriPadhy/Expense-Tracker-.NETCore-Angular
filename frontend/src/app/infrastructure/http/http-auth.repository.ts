import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { AuthResult, User } from '../../domain/models';
import { AuthRepository, LoginRequest, RegisterRequest, UpdateMeRequest } from '../../domain/ports/auth.repository';
import { API_CONFIG } from '../config/api.config';

@Injectable({ providedIn: 'root' })
export class HttpAuthRepository extends AuthRepository {
  private readonly http = inject(HttpClient);
  private readonly cfg = inject(API_CONFIG);
  private readonly base = this.cfg.baseUrl;

  register(req: RegisterRequest, photo?: File): Observable<AuthResult> {
    if (!photo) {
      return this.http.post<AuthResult>(`${this.base}/auth/register-json`, req);
    }
    const fd = new FormData();
    fd.append('fullName', req.fullName);
    fd.append('email', req.email);
    fd.append('phone', req.phone);
    fd.append('password', req.password);
    fd.append('currencyCode', req.currencyCode);
    fd.append('photo', photo);
    return this.http.post<AuthResult>(`${this.base}/auth/register`, fd);
  }

  login(req: LoginRequest): Observable<AuthResult> {
    return this.http.post<AuthResult>(`${this.base}/auth/login`, req);
  }

  refresh(refreshToken: string): Observable<AuthResult> {
    return this.http.post<AuthResult>(`${this.base}/auth/refresh`, { refreshToken });
  }

  logout(refreshToken: string): Observable<void> {
    return this.http.post<void>(`${this.base}/auth/logout`, { refreshToken });
  }

  me(): Observable<User> {
    return this.http.get<User>(`${this.base}/me`);
  }

  updateMe(p: UpdateMeRequest): Observable<User> {
    return this.http.patch<User>(`${this.base}/me`, p);
  }

  uploadPhoto(file: File): Observable<void> {
    const fd = new FormData();
    fd.append('photo', file);
    return this.http.put<void>(`${this.base}/me/photo`, fd).pipe(map(() => undefined as void));
  }

  photoUrl(userId: string): string {
    return `${this.base}/users/${userId}/photo`;
  }
}
