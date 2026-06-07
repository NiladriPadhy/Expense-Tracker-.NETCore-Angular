import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Category, Currency, EntryType } from '../../domain/models';
import { CategoriesRepository } from '../../domain/ports/categories.repository';
import { CurrenciesRepository } from '../../domain/ports/currencies.repository';
import { API_CONFIG } from '../config/api.config';

@Injectable({ providedIn: 'root' })
export class HttpCategoriesRepository extends CategoriesRepository {
  private http = inject(HttpClient);
  private base = inject(API_CONFIG).baseUrl;

  listActive(type?: EntryType): Observable<Category[]> {
    let params = new HttpParams();
    if (type) params = params.set('type', type);
    return this.http.get<Category[]>(`${this.base}/categories`, { params });
  }
}

@Injectable({ providedIn: 'root' })
export class HttpCurrenciesRepository extends CurrenciesRepository {
  private http = inject(HttpClient);
  private base = inject(API_CONFIG).baseUrl;

  listActive(): Observable<Currency[]> {
    return this.http.get<Currency[]>(`${this.base}/currencies/active`);
  }
}
