import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { OrderStatus } from '../../../core/models/api.models';
import { OrdersService } from '../../../core/services/orders.service';
import { I18nService } from '../../../core/services/i18n.service';
import { OrderDetailComponent } from './order-detail.component';

describe('Client three-stage order workflow', () => {
  for (const [status, label, historical] of [
    [OrderStatus.New, 'order.status.new', false],
    [OrderStatus.Preparing, 'order.status.preparing', false],
    [OrderStatus.Ready, 'order.status.ready', false],
    [OrderStatus.Ready, 'order.historical', true],
  ] as const) {
    it(`shows ${label} without a cancellation action`, () => {
      TestBed.configureTestingModule({ imports: [OrderDetailComponent], providers: [provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ orderId: 'order' }) } } },
        { provide: I18nService, useValue: { lang: signal('en'), t: (key: string) => key } },
        { provide: OrdersService, useValue: { getClient: () => of({ id: 'order', status, isHistorical: historical, totalAmount: 3, items: [], orderDateUtc: '2026-10-04T12:00:00Z' }) } },
      ] });
      const fixture = TestBed.createComponent(OrderDetailComponent); fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.badge').textContent).toContain(label);
      expect(fixture.nativeElement.querySelector('button')).toBeNull();
      expect(fixture.nativeElement.textContent).not.toContain('order.cancel');
      if (historical) {
        expect(fixture.nativeElement.textContent).toContain('order.historicalNote');
        expect(fixture.nativeElement.textContent).not.toContain('order.status.ready');
      }
    });
  }
});
