import { Observable } from 'rxjs';
import { Currency } from '../models';

export abstract class CurrenciesRepository {
  abstract listActive(): Observable<Currency[]>;
}
