import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Type, signal } from '@angular/core';
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { CartService } from '../core/services/cart.service';
import { ConfirmService } from '../core/services/confirm.service';
import { I18nService } from '../core/services/i18n.service';
import { ToastService } from '../core/services/toast.service';
import { TokenStoreService } from '../core/services/token-store.service';
import { AdminNotificationsComponent } from '../features/admin/notifications/admin-notifications.component';
import { ProviderNotificationsComponent } from '../features/provider/notifications/provider-notifications.component';
import { ClientNotificationsComponent } from '../features/client/notifications/client-notifications.component';
import { StoreComponent } from '../features/client/store/store.component';
import { HomeComponent } from '../features/client/home/home.component';
import { CartsListComponent } from '../features/client/cart/carts-list.component';
import { CartComponent } from '../features/client/cart/cart.component';
import { ProviderCategoriesComponent } from '../features/provider/categories/provider-categories.component';
import { OrderItemsComponent } from './components/order-items/order-items.component';

describe('System collection pagination', () => {
  let http: HttpTestingController;
  const lang = signal<'en' | 'it' | 'ar'>('en');
  const params = new BehaviorSubject(convertToParamMap({}));
  const category = { id: 'category', name: 'Lunch' };
  const product = { id: 'product', categoryId: 'category', name: 'Bowl', price: 2, imageUrl: null };
  const page = (items: unknown[], totalCount: number, pageSize: number) => ({ items, totalCount, totalPages: Math.ceil(totalCount / pageSize) });

  beforeEach(async () => {
    lang.set('en'); params.next(convertToParamMap({}));
    await TestBed.configureTestingModule({
      imports: [AdminNotificationsComponent, ProviderNotificationsComponent, ClientNotificationsComponent, StoreComponent, HomeComponent, CartsListComponent, CartComponent, ProviderCategoriesComponent, OrderItemsComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ providerId: 'provider' }) }, queryParamMap: params } },
        { provide: I18nService, useValue: { lang, t: (key: string) => key === 'pagination.goTo' ? 'Go to page {page}' : key } },
        { provide: TokenStoreService, useValue: { getUser: () => ({ id: 'client' }), hasPermission: () => true, isAuthenticated: () => true } },
        { provide: ConfirmService, useValue: { ask: () => Promise.resolve(true) } },
        { provide: ToastService, useValue: { success: () => {}, error: () => {}, info: () => {} } },
      ],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('handles a failed notification read request and allows a retry without duplicate requests', () => {
    const fixture = TestBed.createComponent(ClientNotificationsComponent); fixture.detectChanges();
    http.expectOne(request => request.url.endsWith('/notifications/paged')).flush(page([{ id: 'notice', title: 'Order update', body: 'Ready', notificationType: 'order_status_changed', notificationId: null, isRead: false }], 1, 20)); fixture.detectChanges();
    fixture.nativeElement.querySelector('.mark-read').click(); fixture.detectChanges();
    fixture.componentInstance.mark(fixture.componentInstance.items()[0]);
    http.expectOne('/api/v1/client/notifications/notice/read').flush({}, { status: 503, statusText: 'Unavailable' }); fixture.detectChanges();
    expect(fixture.componentInstance.markFailed()).toBeTrue();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('notifications.markFailed');
    fixture.nativeElement.querySelector('.mark-read').click();
    http.expectOne('/api/v1/client/notifications/notice/read').flush(null); fixture.detectChanges();
    expect(fixture.componentInstance.items()[0].isRead).toBeTrue();
    expect(fixture.nativeElement.querySelector('.mark-read')).toBeNull();
  });

  for (const [portal, type] of [['admin', AdminNotificationsComponent], ['provider', ProviderNotificationsComponent], ['client', ClientNotificationsComponent]] as [string, Type<unknown>][]) {
    it(`pages the ${portal} inbox and preserves keyboard focus across loading`, () => {
      const fixture = TestBed.createComponent(type); fixture.detectChanges();
      const first = http.expectOne(request => request.url === `/api/v1/${portal}/notifications/paged`);
      expect(first.request.params.get('pageNumber')).toBe('1'); expect(first.request.params.get('pageSize')).toBe('20');
      first.flush(page([{ id: 'notice-one', title: 'First notice', body: '', isRead: true }], 42, 20)); fixture.detectChanges();
      const button = fixture.nativeElement.querySelector('[aria-label="Go to page 2"]') as HTMLButtonElement;
      button.focus(); button.click(); fixture.detectChanges();
      const second = http.expectOne(request => request.url === `/api/v1/${portal}/notifications/paged`);
      expect(second.request.params.get('pageNumber')).toBe('2');
      second.flush(page([{ id: 'notice-two', title: 'Second notice', body: '', isRead: true }], 42, 20)); fixture.detectChanges();
      expect(fixture.nativeElement.textContent).toContain('Second notice'); expect(fixture.nativeElement.textContent).not.toContain('First notice');
      expect(document.activeElement).toBe(fixture.nativeElement.querySelector('[aria-current="page"]'));
    });
  }

  it('keeps store product/category pages independent and resets product paging on category/search changes', fakeAsync(() => {
    const fixture = TestBed.createComponent(StoreComponent); fixture.detectChanges();
    http.expectOne('/api/v1/client/providers/provider').flush({ id: 'provider', companyName: 'Store' });
    http.expectOne(request => request.url.endsWith('/categories')).flush(page([category], 25, 12));
    http.expectOne(request => request.url.endsWith('/products')).flush(page([product], 25, 12)); fixture.detectChanges();
    http.expectOne('/api/v1/client/provider/cart').flush({ items: [] });
    expect(fixture.nativeElement.querySelector('[aria-label="pagination.categories"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[aria-label="pagination.products"]')).not.toBeNull();
    fixture.componentInstance.loadProducts(2);
    let request = http.expectOne(request => request.url.endsWith('/products')); expect(request.request.params.get('pageNumber')).toBe('2'); request.flush(page([product], 25, 12));
    fixture.componentInstance.selectCategory('category');
    request = http.expectOne(request => request.url.endsWith('/products'));
    expect(request.request.params.get('pageNumber')).toBe('1'); expect(request.request.params.get('categoryId')).toBe('category'); request.flush(page([product], 25, 12));
    fixture.componentInstance.loadCategories(2);
    request = http.expectOne(request => request.url.endsWith('/categories')); expect(request.request.params.get('pageNumber')).toBe('2'); request.flush(page([{ id: 'next-category', name: 'Next category' }], 25, 12));
    http.expectNone(request => request.url.endsWith('/products')); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.chip.active').textContent).toContain('Lunch');
    fixture.componentInstance.loadProducts(3);
    const old = http.expectOne(request => request.url.endsWith('/products'));
    fixture.componentInstance.search.setValue('Soup'); tick(300);
    expect(old.cancelled).toBeTrue(); request = http.expectOne(request => request.url.endsWith('/products'));
    expect(request.request.params.get('pageNumber')).toBe('1'); expect(request.request.params.get('searchTerm')).toBe('Soup'); request.flush(page([], 0, 12));
  }));

  it('loads later marketplace service pages and resets to the first page for a new search', fakeAsync(() => {
    const fixture = TestBed.createComponent(HomeComponent); fixture.detectChanges();
    http.expectOne(request => request.url === '/api/v1/client/services').flush(page([{ id: 'service-one', name: 'First service' }], 25, 12)); fixture.detectChanges();
    fixture.nativeElement.querySelector('[aria-label="Go to page 3"]').click(); fixture.detectChanges();
    let request = http.expectOne(request => request.url === '/api/v1/client/services'); expect(request.request.params.get('pageNumber')).toBe('3'); request.flush(page([{ id: 'service-last', name: 'Last service' }], 25, 12));
    fixture.componentInstance.search.setValue('Food'); tick(300); fixture.detectChanges();
    request = http.expectOne(request => request.url === '/api/v1/client/services'); expect(request.request.params.get('pageNumber')).toBe('1'); expect(request.request.params.get('searchTerm')).toBe('Food'); request.flush(page([], 0, 12));
  }));

  it('pages baskets without replacing the quantity badge with the visible-page quantity', () => {
    const cartService = TestBed.inject(CartService); cartService.itemCount.set(91);
    const fixture = TestBed.createComponent(CartsListComponent); fixture.detectChanges();
    http.expectOne(request => request.url === '/api/v1/client/carts/paged').flush(page([{ id: 'cart', providerId: 'provider', providerName: 'Store', totalPrice: 20, items: [{ id: 'line', productName: 'Bowl', quantity: 2 }] }], 13, 12)); fixture.detectChanges();
    fixture.nativeElement.querySelector('[aria-label="Go to page 2"]').click(); fixture.detectChanges();
    const request = http.expectOne(request => request.url === '/api/v1/client/carts/paged'); expect(request.request.params.get('pageNumber')).toBe('2'); request.flush(page([{ id: 'last-cart', providerId: 'last-provider', providerName: 'Last store', totalPrice: 10, items: [] }], 13, 12));
    expect(cartService.itemCount()).toBe(91);
  });

  it('shows ten basket rows at a time while keeping the full subtotal and quantity', () => {
    const fixture = TestBed.createComponent(CartComponent); fixture.detectChanges();
    const items = Array.from({ length: 23 }, (_, index) => ({ id: String(index), productId: String(index), productName: 'Product ' + index, quantity: 1, price: 2 }));
    http.expectOne('/api/v1/client/provider/cart').flush({ id: 'cart', providerId: 'provider', totalPrice: 46, items }); fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.cart-row').length).toBe(10);
    fixture.nativeElement.querySelector('[aria-label="Go to page 3"]').click(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.cart-row').length).toBe(3); expect(fixture.componentInstance.cart()!.totalPrice).toBe(46);
    expect(fixture.componentInstance.itemQuantity()).toBe(23);
  });

  it('corrects a now-empty last table page and retains a navigation control while the request is pending', () => {
    const fixture = TestBed.createComponent(ProviderCategoriesComponent); fixture.detectChanges();
    http.expectOne(request => request.url === '/api/v1/provider/categories').flush(page([{ ...category, providerId: 'provider', isActive: true, displayOrder: 0 }], 21, 20)); fixture.detectChanges();
    fixture.nativeElement.querySelector('[aria-label="Go to page 2"]').click(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('app-pagination nav')).not.toBeNull();
    http.expectOne(request => request.url === '/api/v1/provider/categories').flush(page([], 20, 20));
    const corrected = http.expectOne(request => request.url === '/api/v1/provider/categories'); expect(corrected.request.params.get('pageNumber')).toBe('1');
    corrected.flush(page([{ ...category, providerId: 'provider', isActive: true, displayOrder: 0 }], 20, 20)); fixture.detectChanges();
    expect(fixture.componentInstance.page).toBe(1); expect(fixture.componentInstance.items().length).toBe(1); expect(fixture.componentInstance.loading()).toBeFalse();
  });

  it('pages order snapshot rows without modifying the supplied order items', () => {
    const fixture = TestBed.createComponent(OrderItemsComponent);
    const items = Array.from({ length: 21 }, (_, index) => ({ id: String(index), productId: String(index), productName: 'Order product ' + index, quantity: 1, unitPrice: 2, lineTotal: 2 }));
    fixture.componentRef.setInput('items', items); fixture.detectChanges();
    expect(fixture.componentInstance.visibleItems().length).toBe(10); fixture.nativeElement.querySelector('[aria-label="Go to page 3"]').click(); fixture.detectChanges();
    expect(fixture.componentInstance.visibleItems().length).toBe(1); expect(items.length).toBe(21); expect(items.reduce((total, item) => total + item.lineTotal, 0)).toBe(42);
  });
});
