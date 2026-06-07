import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { Entry } from '../../domain/models';
import { CreateEntryRequest, EntriesRepository, UpdateEntryRequest } from '../../domain/ports/entries.repository';
import { API_CONFIG } from '../config/api.config';

@Injectable({ providedIn: 'root' })
export class HttpEntriesRepository extends EntriesRepository {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_CONFIG).baseUrl;

  create(req: CreateEntryRequest): Observable<Entry> {
    return this.http.post<Entry>(`${this.base}/entries`, req);
  }
  update(id: string, req: UpdateEntryRequest): Observable<Entry> {
    return this.http.put<Entry>(`${this.base}/entries/${id}`, req);
  }
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/entries/${id}`);
  }
  get(id: string): Observable<Entry> {
    return this.http.get<Entry>(`${this.base}/entries/${id}`);
  }
  listByMonth(_year: number, _month: number): Observable<Entry[]> {
    // Entries are served via GET /months/{year}/{month}.entries — call that endpoint instead.
    return throwError(() => new Error('not_supported'));
  }
}
