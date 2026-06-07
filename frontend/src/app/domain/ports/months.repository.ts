import { Observable } from 'rxjs';
import { MonthlyView } from '../models';

export abstract class MonthsRepository {
  abstract getMonth(year: number, month: number): Observable<MonthlyView>;
}
