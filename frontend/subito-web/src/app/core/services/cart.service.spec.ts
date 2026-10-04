import { I18nService } from './i18n.service';
import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { CartService } from './cart.service';
import { ApiService } from './api.service';
import { TokenStoreService } from './token-store.service';

describe('CartService baskets', () => {
  let service: CartService;
  let api: jasmine.SpyObj<ApiService>;
  beforeEach(() => {
    api = jasmine.createSpyObj<ApiService>('ApiService', ['get', 'delete', 'post', 'put']);
    api.get.and.returnValue(of([]));
    TestBed.configureTestingModule({ providers: [
      { provide: I18nService, useValue: { t: (key: string) => key } },
      CartService, { provide: ApiService, useValue: api },
      { provide: TokenStoreService, useValue: { isAuthenticated: () => true } },
    ] });
    service = TestBed.inject(CartService);
  });
  it('clears only the selected store and refreshes the total quantity badge', () => {
    api.delete.and.returnValue(of(null));
    api.get.and.returnValue(of({ totalQuantity: 5 }));
    service.clear('selected-store').subscribe();
    expect(api.delete).toHaveBeenCalledOnceWith('/client/selected-store/cart');
    expect(service.itemCount()).toBe(5);
    expect(api.get).toHaveBeenCalledWith('/client/carts/count', undefined, { silent: true });
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
    expect(api.post).toHaveBeenCalledOnceWith('/client/store/payments', undefined, undefined, { silent: true });
  });
  it('rejects invalid quantities before calling the API', () => {
    for (const quantity of [0, -1, 1.5, 1000, NaN, Infinity]) {
      let failed = false;
      service.addItem('store', 'product', quantity).subscribe({ error: () => failed = true });
      expect(failed).toBeTrue();
    }
    expect(api.post).not.toHaveBeenCalled();
  });

  it('prevents an older badge request from replacing the latest aggregate quantity', () => {
    const first = new Subject<{ totalQuantity: number }>(); const second = new Subject<{ totalQuantity: number }>();
    api.get.and.returnValues(first, second);
    service.refreshCount(); service.refreshCount();
    expect(first.observed).toBeFalse(); first.next({ totalQuantity: 3 }); second.next({ totalQuantity: 7 });
    expect(service.itemCount()).toBe(7);
  });

});
