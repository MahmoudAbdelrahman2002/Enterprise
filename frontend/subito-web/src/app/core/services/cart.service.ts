import { I18nService } from './i18n.service';
import { VALIDATION_POLICY as P } from '../../shared/forms/validation-policy';
import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { throwError, Observable, Subscription, Subject, catchError, filter, of, shareReplay, startWith, switchMap, tap } from 'rxjs';
import { CreatePaymentSessionDto, ShoppingCartDto, PagedResult } from '../models/domain.models';
import { ApiService, Query } from './api.service';
import { readList } from '../utils/read-list';
import { map } from 'rxjs';
import { TokenStoreService } from './token-store.service';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly i18n = inject(I18nService);
  private readonly api = inject(ApiService);
  private readonly tokens = inject(TokenStoreService);
  private countRequest?: Subscription;
  private readonly basketChanges = new Subject<string>();
  private readonly watchedBaskets = new Map<string, Observable<ShoppingCartDto | null>>();

  constructor() { inject(DestroyRef).onDestroy(() => this.countRequest?.unsubscribe()); }

  /** Total units across all open carts (for header badge). */
  readonly itemCount = signal(0);

  get(providerId: string): Observable<ShoppingCartDto> {
    return this.api.get(`/client/${providerId}/cart`);
  }

  /** Share one store basket request between product badges and reload after successful edits. */
  watchBasket(providerId: string): Observable<ShoppingCartDto | null> {
    const key = `${this.tokens.getUser('client')?.id ?? ''}:${providerId}`;
    let basket = this.watchedBaskets.get(key);
    if (!basket) {
      basket = this.basketChanges.pipe(
        filter(changedProvider => changedProvider === providerId),
        startWith(providerId),
        switchMap(() => this.api.get<ShoppingCartDto>(`/client/${providerId}/cart`, undefined, { silent: true })
          .pipe(catchError(() => of(null)))),
        shareReplay({ bufferSize: 1, refCount: true })
      );
      this.watchedBaskets.set(key, basket);
    }
    return basket;
  }

  private basketChanged(providerId: string): void {
    this.basketChanges.next(providerId);
    this.refreshCount();
  }

  list(): Observable<ShoppingCartDto[]> {
    return this.api.get<ShoppingCartDto[]>('/client/carts', undefined, { silent: true }).pipe(
      map((data) => readList<ShoppingCartDto>(data)),
      tap((carts) => this.itemCount.set(this.countItems(carts)))
    );
  }

  listPage(query: Query): Observable<PagedResult<ShoppingCartDto>> {
    return this.api.get('/client/carts/paged', query);
  }

  refreshCount(): void {
    this.countRequest?.unsubscribe();
    if (!this.tokens.isAuthenticated('client')) {
      this.itemCount.set(0);
      return;
    }
    this.countRequest = this.api.get<{ totalQuantity: number }>('/client/carts/count', undefined, { silent: true }).subscribe({
      next: result => this.itemCount.set(result.totalQuantity),
      error: () => this.itemCount.set(0),
    });
  }

  addItem(providerId: string, productId: string, quantity = 1): Observable<unknown> {
    if (!Number.isSafeInteger(quantity) || quantity < 1 || quantity > P.QuantityMax) return throwError(() => ({ message: this.i18n.t('validation.quantity') }));
    return this.api.post(`/client/${providerId}/cart/items`, { productId, quantity }).pipe(
      tap(() => this.basketChanged(providerId))
    );
  }

  updateItem(providerId: string, itemId: string, quantity: number): Observable<unknown> {
    if (!Number.isSafeInteger(quantity) || quantity < 1 || quantity > P.QuantityMax) return throwError(() => ({ message: this.i18n.t('validation.quantity') }));
    return this.api.put(`/client/${providerId}/cart/items/${itemId}`, { quantity }).pipe(
      tap(() => this.basketChanged(providerId))
    );
  }

  removeItem(providerId: string, itemId: string): Observable<unknown> {
    return this.api.delete(`/client/${providerId}/cart/items/${itemId}`).pipe(
      tap(() => this.basketChanged(providerId))
    );
  }

  clear(providerId: string): Observable<unknown> {
    return this.api.delete(`/client/${providerId}/cart`).pipe(tap(() => this.basketChanged(providerId)));
  }

  checkout(providerId: string): Observable<CreatePaymentSessionDto> {
    return this.api.post(`/client/${providerId}/payments`, undefined, undefined, { silent: true });
  }

  private countItems(carts: ShoppingCartDto[]): number {
    return carts.reduce((sum, c) => sum + (c.items?.reduce((n, i) => n + (i.quantity || 0), 0) || 0), 0);
  }
}
