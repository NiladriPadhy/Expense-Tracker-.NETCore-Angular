import { Observable } from 'rxjs';
import { Dashboard } from '../models';

export abstract class DashboardRepository {
  abstract get(monthsBack?: number): Observable<Dashboard>;
}
