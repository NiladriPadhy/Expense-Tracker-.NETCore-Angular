import { Observable } from 'rxjs';
import { AdminUser, Category, Currency, EntryType, UserRole } from '../models';

export interface PaginatedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface AdminUserUpdateRequest {
  fullName?: string;
  phone?: string;
  currencyCode?: string;
  role?: UserRole;
  isActive?: boolean;
}

export abstract class AdminUsersRepository {
  abstract list(p: { search?: string; page?: number; pageSize?: number }): Observable<PaginatedResult<AdminUser>>;
  abstract get(id: string): Observable<AdminUser>;
  abstract update(id: string, p: AdminUserUpdateRequest): Observable<AdminUser>;
  abstract delete(id: string, hard?: boolean): Observable<void>;
}

export abstract class AdminCategoriesRepository {
  abstract list(): Observable<Category[]>;
  abstract create(p: { name: string; type: EntryType }): Observable<Category>;
  abstract update(id: string, p: { name?: string; isActive?: boolean }): Observable<Category>;
  abstract deactivate(id: string): Observable<void>;
}

export abstract class AdminCurrenciesRepository {
  abstract list(): Observable<Currency[]>;
  abstract create(p: { code: string; name: string; symbol: string }): Observable<Currency>;
  abstract update(code: string, p: { name?: string; symbol?: string; isActive?: boolean }): Observable<Currency>;
  abstract deactivate(code: string, hard?: boolean): Observable<void>;
}
