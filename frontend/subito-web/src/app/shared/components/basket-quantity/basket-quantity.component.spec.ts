import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { CartService } from '../../../core/services/cart.service';
import { I18nService } from '../../../core/services/i18n.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { BasketQuantityComponent } from './basket-quantity.component';

@Component({ standalone: true, imports: [BasketQuantityComponent], template: `
  <app-basket-quantity providerId="store" productId="first" />
  <app-basket-quantity providerId="store" productId="second" />
` })
class ProductCardsFixture {}

describe('Client product basket quantities', () => {
  let http: HttpTestingController;
  const authenticated = signal(true);
  const clientId = signal('client-one');
  const basket = (quantity: number) => ({ items: [{ productId: 'first', quantity }] });
  beforeEach(() => {
    authenticated.set(true);
    clientId.set('client-one');
    TestBed.configureTestingModule({ imports: [ProductCardsFixture], providers: [
      provideHttpClient(), provideHttpClientTesting(),
      { provide: TokenStoreService, useValue: { getUser: () => ({ id: clientId() }), isAuthenticated: () => authenticated() } },
      { provide: I18nService, useValue: { lang: signal('en'), t: (key: string) => key === 'cart.inBasket' ? 'In basket:' : key } },
    ] });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('shares a single request and shows only the product already in the basket', () => {
    const fixture = TestBed.createComponent(ProductCardsFixture); fixture.detectChanges();
    http.expectOne('/api/v1/client/store/cart').flush(basket(3)); fixture.detectChanges();
    const badges = fixture.nativeElement.querySelectorAll('app-basket-quantity');
    expect(badges[0].textContent).toContain('In basket: 3');
    expect(badges[0].querySelector('[role="status"]')).not.toBeNull();
    expect(badges[1].querySelector('[role="status"]')).toBeNull();
  });

  it('reloads authoritative quantities after adding, updating, removing and clearing', () => {
    const fixture = TestBed.createComponent(ProductCardsFixture); fixture.detectChanges();
    http.expectOne('/api/v1/client/store/cart').flush(basket(2));
    const service = TestBed.inject(CartService);
    const edits = [
      () => service.addItem('store', 'first', 1),
      () => service.updateItem('store', 'item', 5),
      () => service.removeItem('store', 'item'),
      () => service.clear('store'),
    ];
    for (const [index, edit] of edits.entries()) {
      edit().subscribe();
      http.expectOne(request => request.method !== 'GET').flush(null);
      http.expectOne('/api/v1/client/store/cart').flush(basket([3, 5, 0, 0][index]));
      http.expectOne('/api/v1/client/carts/count').flush({ totalQuantity: [3, 5, 0, 0][index] });
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelectorAll('[role="status"]').length).toBe(index < 2 ? 1 : 0);
      if (index < 2) expect(fixture.nativeElement.textContent).toContain(`In basket: ${[3, 5][index]}`);
    }
  });

  it('does not request a guest basket and clears quantities when the client signs out', () => {
    authenticated.set(false);
    const fixture = TestBed.createComponent(ProductCardsFixture); fixture.detectChanges();
    http.expectNone('/api/v1/client/store/cart');
    authenticated.set(true); fixture.detectChanges();
    http.expectOne('/api/v1/client/store/cart').flush(basket(4)); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('In basket: 4');
    authenticated.set(false); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="status"]')).toBeNull();
    authenticated.set(true); fixture.detectChanges();
    http.expectOne('/api/v1/client/store/cart').flush({ items: [] }); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="status"]')).toBeNull();
  });

  it('recovers after a failed basket load and cancels pending requests when cards disappear', () => {
    const fixture = TestBed.createComponent(ProductCardsFixture); fixture.detectChanges();
    http.expectOne('/api/v1/client/store/cart').flush({}, { status: 503, statusText: 'Unavailable' });
    fixture.detectChanges(); expect(fixture.nativeElement.querySelector('[role="status"]')).toBeNull();
    TestBed.inject(CartService).addItem('store', 'first').subscribe();
    http.expectOne('/api/v1/client/store/cart/items').flush(null);
    const pending = http.expectOne('/api/v1/client/store/cart');
    http.expectOne('/api/v1/client/carts/count').flush({ totalQuantity: 1 });
    fixture.destroy(); expect(pending.cancelled).toBeTrue();
  });

  it('loads a different client basket without replaying the previous client quantities', () => {
    const fixture = TestBed.createComponent(ProductCardsFixture); fixture.detectChanges();
    http.expectOne('/api/v1/client/store/cart').flush(basket(8)); fixture.detectChanges();
    clientId.set('client-two'); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="status"]')).toBeNull();
    http.expectOne('/api/v1/client/store/cart').flush(basket(1)); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('In basket: 1');
    expect(fixture.nativeElement.textContent).not.toContain('In basket: 8');
  });
});
