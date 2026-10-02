import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { CreatePaymentSessionDto, ShoppingCartDto } from '../models/domain.models';
import { ApiService } from './api.service';
import { readList } from '../utils/read-list';
import { map } from 'rxjs';
import { TokenStoreService } from './token-store.service';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly api = inject(ApiService);
  private readonly tokens = inject(TokenStoreService);

  /** Total line items across all open carts (for header badge). */
  readonly itemCount = signal(0);

  get(providerId: string): Observable<ShoppingCartDto> {
    return this.api.get(`/client/${providerId}/cart`);
  }

  list(): Observable<ShoppingCartDto[]> {
    return this.api.get<ShoppingCartDto[]>('/client/carts', undefined, { silent: true }).pipe(
      map((data) => readList<ShoppingCartDto>(data)),
      tap((carts) => this.itemCount.set(this.countItems(carts)))
    );
  }

  refreshCount(): void {
    if (!this.tokens.isAuthenticated('client')) {
      this.itemCount.set(0);
      return;
    }
    this.list().subscribe({ error: () => this.itemCount.set(0) });
  }

  addItem(providerId: string, productId: string, quantity = 1): Observable<unknown> {
    return this.api.post(`/client/${providerId}/cart/items`, { productId, quantity }).pipe(
      tap(() => this.refreshCount())
    );
  }

  updateItem(providerId: string, itemId: string, quantity: number): Observable<unknown> {
    return this.api.put(`/client/${providerId}/cart/items/${itemId}`, { quantity }).pipe(
      tap(() => this.refreshCount())
    );
  }

  removeItem(providerId: string, itemId: string): Observable<unknown> {
    return this.api.delete(`/client/${providerId}/cart/items/${itemId}`).pipe(
      tap(() => this.refreshCount())
    );
  }

  clear(providerId: string): Observable<unknown> {
    return this.api.delete(`/client/${providerId}/cart`).pipe(tap(() => this.refreshCount()));
  }

  checkout(providerId: string): Observable<CreatePaymentSessionDto> {
    return this.api.post(`/client/${providerId}/payments`);
  }

  private countItems(carts: ShoppingCartDto[]): number {
    return carts.reduce((sum, c) => sum + (c.items?.reduce((n, i) => n + (i.quantity || 0), 0) || 0), 0);
  }
}
