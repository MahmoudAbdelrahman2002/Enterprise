import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { CartService } from './cart.service';
import { ApiService } from './api.service';
import { TokenStoreService } from './token-store.service';

describe('CartService baskets', () => {
  let service: CartService;
  let api: jasmine.SpyObj<ApiService>;
  beforeEach(() => {
    api = jasmine.createSpyObj<ApiService>('ApiService', ['get', 'delete', 'post']);
    api.get.and.returnValue(of([]));
    TestBed.configureTestingModule({ providers: [
      CartService, { provide: ApiService, useValue: api },
      { provide: TokenStoreService, useValue: { isAuthenticated: () => true } },
    ] });
    service = TestBed.inject(CartService);
  });
  it('clears only the selected store and refreshes the total quantity badge', () => {
    api.delete.and.returnValue(of(null));
    api.get.and.returnValue(of({ items: [{ items: [{ quantity: 3 }, { quantity: 2 }] }] }));
    service.clear('selected-store').subscribe();
    expect(api.delete).toHaveBeenCalledOnceWith('/client/selected-store/cart');
    expect(service.itemCount()).toBe(5);
  });
  it('propagates list failures so the basket page can display retry', () => {
    const error = new Error('unavailable');
    api.get.and.returnValue(throwError(() => error));
    let received: unknown;
    service.list().subscribe({ error: (e) => received = e });
    expect(received).toBe(error);
  });
  it('continues to request hosted checkout for one store', () => {
    const session = { sessionId: 'session', url: 'https://checkout.stripe.com/test', metadata: {} };
    api.post.and.returnValue(of(session));
    service.checkout('store').subscribe((result) => expect(result).toEqual(session));
    expect(api.post).toHaveBeenCalledOnceWith('/client/store/payments');
  });
});
