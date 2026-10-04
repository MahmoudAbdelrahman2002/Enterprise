import { DestroyRef } from '@angular/core';
import { Observable, PartialObserver, Subscription } from 'rxjs';

/** A list keeps only its latest request; replacing or destroying it cancels stale responses. */
export class PageRequest {
  private request?: Subscription;
  private generation = 0;
  constructor(destroyRef: DestroyRef) {
    destroyRef.onDestroy(() => { this.generation++; this.request?.unsubscribe(); });
  }
  run<T>(source: Observable<T>, observer: PartialObserver<T>): void {
    this.request?.unsubscribe();
    const generation = ++this.generation;
    const request = source.subscribe(observer);
    // A synchronous response can cause an out-of-range page to load another page.
    if (generation === this.generation) this.request = request;
    else request.unsubscribe();
  }
}
