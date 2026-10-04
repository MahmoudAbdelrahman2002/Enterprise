import { Component, signal } from '@angular/core';
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Portal } from '../../../core/models/api.models';
import { NotificationDto } from '../../../core/models/domain.models';
import { I18nService } from '../../../core/services/i18n.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { NotificationCardComponent } from './notification-card.component';

@Component({ standalone: true, template: '' })
class DestinationFixture {}

describe('Notification cards and order destinations', () => {
  const orderId = 'bdf02bee-c1d7-43ee-a42c-d2c7302d121a';
  let permitted: boolean;
  const notification: NotificationDto = { id: 'notice', title: 'Order status updated', body: 'Your order is now Ready.', notificationType: 'order_status_changed', notificationId: orderId, isRead: false, createdAtUtc: '2026-10-04T12:00:00Z' };
  beforeEach(() => {
    permitted = true;
    TestBed.configureTestingModule({ imports: [NotificationCardComponent], providers: [
      provideRouter([{ path: 'orders/:orderId', component: DestinationFixture }, { path: 'provider/orders/:orderId', component: DestinationFixture }, { path: 'admin/providers/:id', component: DestinationFixture }]),
      { provide: I18nService, useValue: { lang: signal('en'), t: (key: string) => key } },
      { provide: TokenStoreService, useValue: { hasPermission: () => permitted } },
    ] });
  });
  function card(portal: Portal, data: NotificationDto = notification) {
    const fixture = TestBed.createComponent(NotificationCardComponent);
    fixture.componentRef.setInput('portal', portal); fixture.componentRef.setInput('notification', data); fixture.detectChanges();
    return fixture;
  }

  for (const portal of ['client', 'provider'] as const) {
    it(`opens the exact ${portal} order from a notification`, fakeAsync(() => {
      const fixture = card(portal);
      const read = jasmine.createSpy(); fixture.componentInstance.read.subscribe(read);
      const link = fixture.nativeElement.querySelector('a') as HTMLAnchorElement;
      expect(link.textContent).toContain(notification.title);
      expect(link.textContent).toContain('notifications.openOrder');
      expect(link.getAttribute('aria-label')).toContain('#bdf02bee');
      expect(fixture.nativeElement.textContent).toContain('#bdf02bee');
      expect(fixture.nativeElement.querySelector('time').getAttribute('datetime')).toBe(notification.createdAtUtc);
      link.click(); tick();
      expect(TestBed.inject(Router).url).toBe(`${portal === 'provider' ? '/provider' : ''}/orders/${orderId}`);
      expect(read).toHaveBeenCalledOnceWith(notification);
    }));
  }

  it('expands order details without linking to a page the Provider cannot read', () => {
    permitted = false;
    const fixture = card('provider');
    expect(fixture.nativeElement.querySelector('a')).toBeNull();
    const button = fixture.nativeElement.querySelector('.notification-toggle') as HTMLButtonElement;
    button.click(); fixture.detectChanges();
    expect(button.getAttribute('aria-expanded')).toBe('true');
    expect(fixture.nativeElement.querySelector('.notification-body').classList.contains('preview')).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('notifications.orderAccessUnavailable');
    button.click(); fixture.detectChanges(); expect(button.getAttribute('aria-expanded')).toBe('false');
  });

  it('keeps Admin order access disabled while linking permitted store registrations', () => {
    const fixture = card('admin'); expect(fixture.nativeElement.querySelector('a')).toBeNull();
    fixture.componentRef.setInput('notification', { ...notification, notificationType: 'new_provider_registration' }); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('a').getAttribute('href')).toBe(`/admin/providers/${orderId}`);
    permitted = false; fixture.detectChanges(); expect(fixture.nativeElement.querySelector('a')).toBeNull();
  });

  it('provides expandable messages for missing, invalid and unsupported destinations', () => {
    for (const data of [
      { ...notification, notificationId: null },
      { ...notification, notificationId: 'javascript:alert(1)' },
      { ...notification, notificationId: '00000000-0000-0000-0000-000000000000' },
      { ...notification, notificationType: 'other_event' },
    ]) {
      const fixture = card('client', data);
      expect(fixture.nativeElement.querySelector('a')).toBeNull();
      fixture.nativeElement.querySelector('.notification-toggle').click(); fixture.detectChanges();
      expect(fixture.nativeElement.textContent).toContain(notification.body);
      fixture.destroy();
    }
  });

  it('keeps the explicit mark-read action separate and disables it during a request', () => {
    const fixture = card('client'); const read = jasmine.createSpy(); fixture.componentInstance.read.subscribe(read);
    fixture.componentRef.setInput('marking', true); fixture.detectChanges();
    fixture.nativeElement.querySelector('.mark-read').click(); expect(read).not.toHaveBeenCalled();
    fixture.componentRef.setInput('marking', false); fixture.detectChanges();
    fixture.nativeElement.querySelector('.mark-read').click(); expect(read).toHaveBeenCalledOnceWith(notification);
    fixture.componentRef.setInput('notification', { ...notification, isRead: true }); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.mark-read')).toBeNull();
    expect(fixture.nativeElement.querySelector('article').classList.contains('notification-unread')).toBeFalse();
  });
});
