import { DatePipe } from '@angular/common';
import { Component, EventEmitter, Input, Output, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Portal } from '../../../core/models/api.models';
import { NotificationDto } from '../../../core/models/domain.models';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { TranslatePipe } from '../../pipes/translate.pipe';
import { IconComponent, IconName } from '../icon/icon.component';

@Component({
  selector: 'app-notification-card',
  standalone: true,
  imports: [DatePipe, RouterLink, TranslatePipe, IconComponent],
  template: `
    <article class="card notification-card" [class.notification-unread]="!notification.isRead">
      <div class="notification-icon" [class.order-icon]="isOrder"><app-icon [name]="icon" /></div>
      <div class="notification-content">
        <div class="notification-meta">
          <span class="event-label">{{ eventLabel | t }}</span>
          <span class="read-state" [class.unread]="!notification.isRead"><app-icon [name]="notification.isRead ? 'check' : 'bell'" />{{ (notification.isRead ? 'notifications.read' : 'notifications.unread') | t }}</span>
        </div>
        @if (destination; as link) {
          <a class="notification-main" [routerLink]="link" [attr.aria-label]="notification.title + ' — ' + ((isOrder ? 'notifications.openOrder' : 'notifications.openProvider') | t) + ' #' + notification.notificationId!.slice(0, 8)" (click)="read.emit(notification)">
            <h2>{{ notification.title }}</h2>
            <p class="notification-body">{{ notification.body }}</p>
            <span class="notification-action">{{ (isOrder ? 'notifications.openOrder' : 'notifications.openProvider') | t }}<app-icon class="directional" name="right" /></span>
          </a>
        } @else {
          <button class="notification-main notification-toggle" type="button" [attr.aria-expanded]="expanded()" [attr.aria-controls]="'notification-body-' + notification.id" (click)="toggle()">
            <h2>{{ notification.title }}</h2>
            <span class="notification-action">{{ (expanded() ? 'notifications.hideDetails' : 'notifications.showDetails') | t }}<app-icon [name]="expanded() ? 'minus' : 'plus'" /></span>
          </button>
          <p class="notification-body" [class.preview]="!expanded()" [id]="'notification-body-' + notification.id">{{ notification.body }}</p>
          @if (expanded() && isOrder) {
            <p class="muted access-note">{{ (validRelatedId ? 'notifications.orderAccessUnavailable' : 'notifications.orderLinkUnavailable') | t }}</p>
          }
        }
        <div class="notification-footer">
          @if (isOrder && validRelatedId) { <span class="order-reference">{{ 'notifications.orderReference' | t }} #{{ notification.notificationId!.slice(0, 8) }}</span> }
          <time class="muted" [attr.datetime]="notification.createdAtUtc">{{ notification.createdAtUtc | date:'medium' }}</time>
          @if (!notification.isRead) {
            <button class="btn btn-ghost mark-read" type="button" [disabled]="marking" (click)="read.emit(notification)">{{ (marking ? 'loading' : 'notifications.markRead') | t }}</button>
          }
        </div>
      </div>
    </article>
  `,
  styles: [`
    .notification-card{display:flex;align-items:flex-start;gap:1rem;padding:1.1rem;border:1px solid var(--subito-border);border-inline-start:4px solid var(--subito-border)}
    .notification-unread{border-inline-start-color:var(--subito-teal)}
    .notification-icon{display:grid;place-items:center;flex:0 0 2.75rem;height:2.75rem;border-radius:14px;background:var(--subito-page);color:var(--subito-navy)}
    .order-icon{background:var(--subito-teal-soft)}
    .notification-content{min-width:0;flex:1}.notification-meta,.notification-footer{display:flex;flex-wrap:wrap;align-items:center;gap:.5rem 1rem}
    .notification-meta{justify-content:space-between;margin-bottom:.5rem;font-size:.8rem}.event-label{font-weight:700;color:var(--subito-navy)}
    .read-state{display:inline-flex;align-items:center;gap:.3rem;color:var(--subito-muted)}.read-state app-icon{width:1rem;height:1rem}.read-state.unread{color:var(--subito-navy);font-weight:700}
    .notification-main{display:block;color:inherit;text-decoration:none;border-radius:6px;overflow-wrap:anywhere}.notification-main:hover h2{text-decoration:underline}
    h2{margin:0;font-size:1.05rem;line-height:1.45}.notification-body{margin:.5rem 0;white-space:pre-wrap;overflow-wrap:anywhere;line-height:1.6}
    .notification-toggle{border:0;background:none;padding:0;text-align:start;width:100%;cursor:pointer;font:inherit}
    .notification-action{display:flex;align-items:center;gap:.4rem;margin-top:.6rem;font-size:.875rem;font-weight:700;color:var(--subito-navy);min-height:2rem}
    .preview{display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden}
    .notification-footer{margin-top:.8rem;font-size:.8rem}.order-reference{font-weight:600}.mark-read{margin-inline-start:auto;min-height:44px}.access-note{font-size:.875rem}
    @media(max-width:560px){.notification-card{gap:.65rem;padding:.8rem}.notification-icon{flex-basis:2.25rem;height:2.25rem}.notification-meta{align-items:flex-start}.notification-footer{gap:.5rem}.mark-read{margin-inline-start:0}}
  `],
})
export class NotificationCardComponent {
  @Input({ required: true }) notification!: NotificationDto;
  @Input({ required: true }) portal!: Portal;
  @Input() marking = false;
  @Output() read = new EventEmitter<NotificationDto>();
  readonly expanded = signal(false);
  private readonly tokens = inject(TokenStoreService);

  get isOrder(): boolean { return ['new_order', 'order_status_changed'].includes(this.notification.notificationType); }
  get validRelatedId(): boolean {
    const id = this.notification.notificationId ?? '';
    return id !== '00000000-0000-0000-0000-000000000000' && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(id);
  }
  get icon(): IconName { return this.isOrder ? 'receipt' : this.notification.notificationType === 'new_provider_registration' ? 'store' : 'bell'; }
  get eventLabel(): string {
    return this.notification.notificationType === 'new_order' ? 'notifications.newOrder'
      : this.notification.notificationType === 'order_status_changed' ? 'notifications.orderUpdate'
      : this.notification.notificationType === 'new_provider_registration' ? 'notifications.newProvider' : 'notifications.activity';
  }
  get destination(): string[] | null {
    if (!this.validRelatedId) return null;
    const id = this.notification.notificationId!;
    if (this.isOrder && this.portal === 'client') return ['/orders', id];
    if (this.isOrder && this.portal === 'provider' && this.tokens.hasPermission('provider', 'ProviderOrder.Read')) return ['/provider/orders', id];
    if (this.portal === 'admin' && this.notification.notificationType === 'new_provider_registration' && this.tokens.hasPermission('admin', 'Providers.Read')) return ['/admin/providers', id];
    return null;
  }
  toggle(): void { this.expanded.update(value => !value); if (this.expanded()) this.read.emit(this.notification); }
}
