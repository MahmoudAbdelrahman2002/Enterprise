import { EMPTY, Observable, expand, map, reduce } from 'rxjs';
import { readPage } from './read-list';

/** Form lookups need every selectable record, even when the underlying list API is paged. */
export function readAllPages<T>(fetch: (page: number) => Observable<unknown>): Observable<T[]> {
  const request = (page: number) => fetch(page).pipe(map(data => ({ page, ...readPage<T>(data) })));
  return request(1).pipe(
    expand(result => result.page < result.totalPages ? request(result.page + 1) : EMPTY, 1),
    map(result => result.items),
    reduce((items, page) => items.concat(page), [] as T[]),
  );
}
