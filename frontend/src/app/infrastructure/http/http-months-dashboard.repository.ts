import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Dashboard, MonthlyView } from '../../domain/models';
import { DashboardRepository } from '../../domain/ports/dashboard.repository';
import { MonthsRepository } from '../../domain/ports/months.repository';
import { API_CONFIG } from '../config/api.config';

@Injectable({ providedIn: 'root' })
export class HttpMonthsRepository extends MonthsRepository {
  private http = inject(HttpClient);
  private base = inject(API_CONFIG).baseUrl;

  getMonth(year: number, month: number): Observable<MonthlyView> {
    return this.http.get<MonthlyView>(`${this.base}/months/${year}/${month}`);
  }
}

@Injectable({ providedIn: 'root' })
export class HttpDashboardRepository extends DashboardRepository {
  private http = inject(HttpClient);
  private base = inject(API_CONFIG).baseUrl;

  get(monthsBack = 6): Observable<Dashboard> {
    return this.http.get<Dashboard>(`${this.base}/dashboard`, { params: { monthsBack } });
  }
}
