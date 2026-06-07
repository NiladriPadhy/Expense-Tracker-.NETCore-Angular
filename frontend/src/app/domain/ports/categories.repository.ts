import { Observable } from 'rxjs';
import { Category, EntryType } from '../models';

export abstract class CategoriesRepository {
  abstract listActive(type?: EntryType): Observable<Category[]>;
}
