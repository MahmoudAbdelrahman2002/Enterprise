import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { I18nService } from '../../../core/services/i18n.service';
import { OrdersService } from '../../../core/services/orders.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { ProviderOrdersComponent } from './provider-orders.component';
import { OrderStatus } from '../../../core/models/api.models';

describe('Provider notification order destination', () => {
  it('retries the exact order after a failed deep link instead of loading the order list', () => {
    const id = 'bdf02bee-c1d7-43ee-a42c-d2c7302d121a';
    const orders = jasmine.createSpyObj<OrdersService>('OrdersService', ['getProvider', 'listProvider']);
    orders.getProvider.and.returnValues(throwError(() => ({ statusCode: 503 })), of({ id, providerId: 'provider', userId: 'client', status: 0, orderDateUtc: '2026-10-04T12:00:00Z', totalAmount: 2, items: [] }));
    TestBed.configureTestingModule({ imports: [ProviderOrdersComponent], providers: [provideRouter([]),
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ orderId: id }) } } },
      { provide: OrdersService, useValue: orders },
      { provide: TokenStoreService, useValue: { hasPermission: () => false } },
      { provide: I18nService, useValue: { lang: signal('en'), t: (key: string) => key } },
    ] });
    const fixture = TestBed.createComponent(ProviderOrdersComponent); fixture.detectChanges();
    expect(fixture.componentInstance.failed()).toBeTrue();
    expect(fixture.nativeElement.querySelector('a[href="/provider/orders"]')).not.toBeNull();
    fixture.nativeElement.querySelector('.error-state button').click(); fixture.detectChanges();
    expect(orders.getProvider.calls.allArgs()).toEqual([[id], [id]]);
    expect(orders.listProvider).not.toHaveBeenCalled();
    expect(fixture.componentInstance.selected()?.id).toBe(id);
    expect(fixture.componentInstance.failed()).toBeFalse();
    expect(fixture.componentInstance.nextStatuses).toEqual([OrderStatus.Preparing]);
    fixture.componentInstance.selected.update(order => ({ ...order!, status: OrderStatus.Preparing }));
    expect(fixture.componentInstance.nextStatuses).toEqual([OrderStatus.Ready]);
    fixture.componentInstance.selected.update(order => ({ ...order!, status: OrderStatus.Ready }));
    expect(fixture.componentInstance.nextStatuses).toEqual([]);
    fixture.componentInstance.selected.update(order => ({ ...order!, status: OrderStatus.New, isHistorical: true }));
    expect(fixture.componentInstance.nextStatuses).toEqual([]);
  });
});
