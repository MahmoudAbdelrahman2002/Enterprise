import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed, fakeAsync, flushMicrotasks } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { ConfirmService } from '../core/services/confirm.service';
import { I18nService } from '../core/services/i18n.service';
import { TokenStoreService } from '../core/services/token-store.service';
import { ToastService } from '../core/services/toast.service';
import { StoreComponent } from '../features/client/store/store.component';
import { ProviderCategoriesComponent } from '../features/provider/categories/provider-categories.component';
import { ProviderProductsComponent } from '../features/provider/products/provider-products.component';
import { CartComponent } from '../features/client/cart/cart.component';
import { CartCountPipe } from './pipes/cart-count.pipe';

describe('QA frontend recovery and feedback', () => {
  let http: HttpTestingController;
  let toast: jasmine.SpyObj<ToastService>;
  let confirm: jasmine.SpyObj<ConfirmService>;
  const lang = signal<'en' | 'it' | 'ar'>('en');
  const category = { id: 'category', providerId: 'provider', name: 'Lunch bowls', displayOrder: 0, isActive: true, imageUrl: null };
  const product = { id: 'product', categoryId: 'category', name: 'Bowl', sku: 'BOWL', price: 12.5, status: 1, imageUrl: null };
  const basket = { id: 'basket', providerId: 'provider', providerName: 'Store', totalPrice: 25, items: [{ id: 'line', productId: 'product', productName: 'Bowl', quantity: 2, price: 12.5, productImage: null }] };

  beforeEach(async () => {
    lang.set('en');
    toast = jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error', 'info']);
    confirm = jasmine.createSpyObj<ConfirmService>('ConfirmService', ['ask']);
    confirm.ask.and.returnValue(Promise.resolve(true));
    await TestBed.configureTestingModule({
      imports: [StoreComponent, ProviderCategoriesComponent, ProviderProductsComponent, CartComponent],
      providers: [
        provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ providerId: 'provider' }) } } },
        { provide: ConfirmService, useValue: confirm },
        { provide: ToastService, useValue: toast },
        { provide: TokenStoreService, useValue: { getUser: () => ({ id: 'client' }), hasPermission: () => true, isAuthenticated: () => true } },
        { provide: I18nService, useValue: { lang, t: (key: string, fallback?: string) => key.startsWith('cart.product.') ? '{count} products' : key.startsWith('cart.unit.') ? '{count} units' : key || fallback } },
      ],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('shows one unavailable-store state without requesting its categories or products', () => {
    const fixture = TestBed.createComponent(StoreComponent);
    fixture.detectChanges();
    http.expectOne('/api/v1/client/providers/provider').flush({ message: 'Entity Provider was not found' }, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();
    expect(fixture.componentInstance.unavailable()).toBeTrue();
    expect(fixture.nativeElement.textContent).toContain('store.unavailable');
    expect(fixture.nativeElement.querySelector('app-empty-state a').getAttribute('href')).toBe('/');
    expect(fixture.nativeElement.querySelector('.chip-scroll')).toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('Entity');
    http.expectNone(request => request.url.includes('/provider/categories') || request.url.includes('/provider/products'));
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('offers a working retry for a transient store failure', () => {
    const fixture = TestBed.createComponent(StoreComponent);
    fixture.detectChanges();
    http.expectOne('/api/v1/client/providers/provider').flush({}, { status: 503, statusText: 'Unavailable' });
    fixture.detectChanges();
    expect(fixture.componentInstance.failed()).toBeTrue();
    fixture.nativeElement.querySelector('.error-state button').click();
    http.expectOne('/api/v1/client/providers/provider').flush({ id: 'provider', companyName: 'Store' });
    http.expectOne(request => request.url === '/api/v1/client/provider/categories').flush([]);
    http.expectOne(request => request.url === '/api/v1/client/provider/products').flush([]);
    fixture.detectChanges();
    expect(fixture.componentInstance.failed()).toBeFalse();
    expect(fixture.componentInstance.store()?.companyName).toBe('Store');
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('names a category and gives a safe recovery path when deletion is blocked by products', fakeAsync(() => {
    const fixture = TestBed.createComponent(ProviderCategoriesComponent);
    fixture.detectChanges();
    http.expectOne(request => request.url === '/api/v1/provider/categories').flush([category]);
    fixture.componentInstance.remove(category.id); flushMicrotasks();
    expect(confirm.ask).toHaveBeenCalledWith('confirm.deleteCategory', category.name);
    const request = http.expectOne('/api/v1/provider/categories/category');
    expect(request.request.method).toBe('DELETE');
    expect(request.request.params.has('deleteRelatedProducts')).toBeFalse();
    request.flush({ message: 'Call again with deleteRelatedProducts=true' }, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('category.hasProducts');
    expect(fixture.nativeElement.textContent).not.toContain('deleteRelatedProducts');
    expect(fixture.nativeElement.querySelector('[role="alert"] a').getAttribute('href')).toBe('/provider/products');
    expect(fixture.componentInstance.items().length).toBe(1);
    expect(toast.error).not.toHaveBeenCalled();
  }));

  it('refreshes the category row thumbnail after upload succeeds', () => {
    const fixture = TestBed.createComponent(ProviderCategoriesComponent);
    fixture.detectChanges();
    http.expectOne(request => request.url === '/api/v1/provider/categories').flush([category]);
    fixture.componentInstance.upload(category.id, new File(['image'], 'image.png', { type: 'image/png' }));
    http.expectOne('/api/v1/provider/categories/category/image').flush({});
    http.expectOne(request => request.url === '/api/v1/provider/categories').flush([{ ...category, imageUrl: '/assets/category.png' }]);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('td img').getAttribute('src')).toBe('/assets/category.png');
    expect(fixture.nativeElement.querySelector('td img').getAttribute('alt')).toBe(category.name);
  });

  it('refreshes the product row thumbnail after upload succeeds', () => {
    const fixture = TestBed.createComponent(ProviderProductsComponent);
    fixture.detectChanges();
    http.expectOne('/api/v1/provider/categories/lookup').flush([category]);
    http.expectOne(request => request.url === '/api/v1/provider/products').flush({ items: [product] });
    fixture.componentInstance.upload(product, new File(['image'], 'image.png', { type: 'image/png' }));
    http.expectOne('/api/v1/provider/categories/category/products/product/image').flush({});
    http.expectOne(request => request.url === '/api/v1/provider/products').flush({ items: [{ ...product, imageUrl: '/assets/product.png' }] });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('td img').getAttribute('src')).toBe('/assets/product.png');
    expect(fixture.nativeElement.querySelector('td a').getAttribute('href')).toBe('/assets/product.png');
  });

  it('preserves the basket and gives payment-specific feedback when checkout fails', () => {
    const fixture = TestBed.createComponent(CartComponent);
    fixture.detectChanges();
    http.expectOne('/api/v1/client/provider/cart').flush(basket);
    fixture.componentInstance.checkout();
    const request = http.expectOne('/api/v1/client/provider/payments');
    fixture.componentInstance.checkout();
    http.expectNone('/api/v1/client/provider/payments');
    request.flush({ message: 'An unexpected error occurred' }, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('cart.checkoutFailed');
    expect(fixture.componentInstance.cart()?.items[0].quantity).toBe(2);
    expect(fixture.componentInstance.busy()).toBeFalse();
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('selects locale plural forms independently for products and units', () => {
    const i18n = TestBed.inject(I18nService);
    const translate = spyOn(i18n, 't').and.callFake((key: string) => key);
    const pipe = TestBed.runInInjectionContext(() => new CartCountPipe());
    pipe.transform(1, 'product'); expect(translate).toHaveBeenCalledWith('cart.product.one', 'cart.product.other');
    pipe.transform(2); expect(translate).toHaveBeenCalledWith('cart.unit.other', 'cart.unit.other');
    lang.set('ar'); pipe.transform(2); expect(translate).toHaveBeenCalledWith('cart.unit.two', 'cart.unit.other');
    lang.set('it'); pipe.transform(1); expect(translate).toHaveBeenCalledWith('cart.unit.one', 'cart.unit.other');
  });
});
