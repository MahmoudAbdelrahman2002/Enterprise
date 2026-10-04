import { DestroyRef } from '@angular/core';
import { Subject, of, throwError } from 'rxjs';
import { PageRequest } from './page-request';
import { readAllPages } from './read-all-pages';

describe('Paged requests and form lookups', () => {
  it('cancels superseded responses and cancels the active request when its view is destroyed', () => {
    let destroy!: () => void;
    const request = new PageRequest({ onDestroy: (callback: () => void) => { destroy = callback; return () => {}; } } as DestroyRef);
    const first = new Subject<number>(); const second = new Subject<number>(); const next = jasmine.createSpy();
    request.run(first, { next }); request.run(second, { next });
    expect(first.observed).toBeFalse(); first.next(1); second.next(2); expect(next).toHaveBeenCalledOnceWith(2);
    destroy(); expect(second.observed).toBeFalse();
  });
  it('keeps a corrected page active when an out-of-range cached response loads it synchronously', () => {
    const request = new PageRequest({ onDestroy: () => () => {} } as unknown as DestroyRef);
    const corrected = new Subject<number>(); const next = jasmine.createSpy();
    request.run(of(0), { next: () => request.run(corrected, { next }) });
    corrected.next(5); expect(next).toHaveBeenCalledOnceWith(5);
    expect(corrected.observed).toBeTrue();
  });
  it('collects every lookup page so roles after the first hundred remain selectable', () => {
    const fetch = jasmine.createSpy().and.callFake((page: number) => of({ items: [page], totalPages: 3, totalCount: 3 }));
    let result: number[] = []; readAllPages<number>(fetch).subscribe(items => result = items);
    expect(result).toEqual([1, 2, 3]); expect(fetch.calls.allArgs()).toEqual([[1], [2], [3]]);
  });
  it('reports later lookup failures instead of presenting a silently truncated list', () => {
    const fetch = (page: number) => page === 1 ? of({ items: [1], totalPages: 2 }) : throwError(() => new Error('offline'));
    let failed = false; const next = jasmine.createSpy(); readAllPages<number>(fetch).subscribe({ next, error: () => failed = true });
    expect(failed).toBeTrue(); expect(next).not.toHaveBeenCalled();
  });
});
