import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminUser, Category, Currency, EntryType } from '../../domain/models';
import {
  AdminCategoriesRepository,
  AdminCurrenciesRepository,
  AdminUserUpdateRequest,
  AdminUsersRepository,
  PaginatedResult,
} from '../../domain/ports/admin.repository';
import { API_CONFIG } from '../config/api.config';

@Injectable({ providedIn: 'root' })
export class HttpAdminUsersRepository extends AdminUsersRepository {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_CONFIG).baseUrl;

  list(p: { search?: string; page?: number; pageSize?: number }): Observable<PaginatedResult<AdminUser>> {
    let params = new HttpParams();
    if (p.search) params = params.set('search', p.search);
    if (p.page) params = params.set('page', String(p.page));
    if (p.pageSize) params = params.set('pageSize', String(p.pageSize));
    return this.http.get<PaginatedResult<AdminUser>>(`${this.base}/admin/users`, { params });
  }

  get(id: string): Observable<AdminUser> {
    return this.http.get<AdminUser>(`${this.base}/admin/users/${id}`);
  }

  update(id: string, p: AdminUserUpdateRequest): Observable<AdminUser> {
    return this.http.patch<AdminUser>(`${this.base}/admin/users/${id}`, p);
  }

  delete(id: string, hard = false): Observable<void> {
    let params = new HttpParams();
    if (hard) params = params.set('hard', 'true');
    return this.http.delete<void>(`${this.base}/admin/users/${id}`, { params });
  }
}

@Injectable({ providedIn: 'root' })
export class HttpAdminCategoriesRepository extends AdminCategoriesRepository {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_CONFIG).baseUrl;

  list(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.base}/admin/categories`);
  }
  create(p: { name: string; type: EntryType }): Observable<Category> {
    return this.http.post<Category>(`${this.base}/admin/categories`, p);
  }
  update(id: string, p: { name?: string; isActive?: boolean }): Observable<Category> {
    return this.http.put<Category>(`${this.base}/admin/categories/${id}`, p);
  }
  deactivate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/admin/categories/${id}`);
  }
}

@Injectable({ providedIn: 'root' })
export class HttpAdminCurrenciesRepository extends AdminCurrenciesRepository {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_CONFIG).baseUrl;

  list(): Observable<Currency[]> {
    return this.http.get<Currency[]>(`${this.base}/admin/currencies`);
  }
  create(p: { code: string; name: string; symbol: string }): Observable<Currency> {
    return this.http.post<Currency>(`${this.base}/admin/currencies`, p);
  }
  update(code: string, p: { name?: string; symbol?: string; isActive?: boolean }): Observable<Currency> {
    return this.http.put<Currency>(`${this.base}/admin/currencies/${code}`, p);
  }
  deactivate(code: string, hard = false): Observable<void> {
    let params = new HttpParams();
    if (hard) params = params.set('hard', 'true');
    return this.http.delete<void>(`${this.base}/admin/currencies/${code}`, { params });
  }
}
