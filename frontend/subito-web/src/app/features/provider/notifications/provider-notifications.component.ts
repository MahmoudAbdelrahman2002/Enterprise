import { IconComponent } from '../../../shared/components/icon/icon.component';
import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { NotificationDto } from '../../../core/models/domain.models';
import { NotificationsService } from '../../../core/services/notifications.service';
import { readList } from '../../../core/utils/read-list';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-provider-notifications',
  standalone: true,
  imports: [IconComponent, DatePipe, EmptyStateComponent, TranslatePipe],
  template: `
    <h1 class="page-title">{{ 'nav.notifications' | t }}</h1>
    @if (loading()) { <p class="muted">{{ 'loading' | t }}</p> }
    @else if (failed()) { <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost" type="button" (click)="ngOnInit()">{{ 'actions.retry' | t }}</button></div> } @else if (!items().length) { <app-empty-state /> } @else {
      <div class="stack">
        @for (n of items(); track n.id) {
          <article class="card" [class.notification-unread]="!n.isRead">
            <strong>{{ n.title }}</strong><p>{{ n.body }}</p>
            <span class="muted">{{ n.createdAtUtc | date:'medium' }}</span>
          <div class="row" style="margin-top:.75rem">@if (!n.isRead) { <button class="btn btn-ghost" type="button" (click)="mark(n)"><app-icon name="bell" />{{ 'notifications.markRead' | t }}</button> } @else { <span class="badge">{{ 'notifications.read' | t }}</span> }</div></article>
        }
      </div>
    }
  `,
})
export class ProviderNotificationsComponent implements OnInit {
  private readonly notifications = inject(NotificationsService);
  readonly items = signal<NotificationDto[]>([]);
  readonly loading = signal(true);
  readonly failed = signal(false);
  ngOnInit(): void {
    this.notifications.list('provider').subscribe({
      next: (res) => { this.items.set(readList<NotificationDto>(res)); this.loading.set(false); },
      error: () => { this.failed.set(true); this.loading.set(false); },
    });
  }
  mark(n: NotificationDto): void {
    if (n.isRead) return;
    this.notifications.markRead('provider', n.id).subscribe({
      next: () => this.items.update((list) => list.map((x) => x.id === n.id ? { ...x, isRead: true } : x)),
    });
  }
}
