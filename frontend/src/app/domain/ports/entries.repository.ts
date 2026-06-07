import { Observable } from 'rxjs';
import { Entry, EntryType } from '../models';

export interface CreateEntryRequest {
  entryDate: string;
  type: EntryType;
  amount: number;
  categoryId?: string | null;
  categoryFreeText?: string | null;
  note?: string | null;
}

export type UpdateEntryRequest = CreateEntryRequest;

export abstract class EntriesRepository {
  abstract create(req: CreateEntryRequest): Observable<Entry>;
  abstract update(id: string, req: UpdateEntryRequest): Observable<Entry>;
  abstract delete(id: string): Observable<void>;
  abstract get(id: string): Observable<Entry>;
  abstract listByMonth(year: number, month: number): Observable<Entry[]>;
}
