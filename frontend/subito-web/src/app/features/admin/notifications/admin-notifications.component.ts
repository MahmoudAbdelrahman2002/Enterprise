import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { PageRequest } from '../../../core/utils/page-request';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { NotificationCardComponent } from '../../../shared/components/notification-card/notification-card.component';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { NotificationDto } from '../../../core/models/domain.models';
import { NotificationsService } from '../../../core/services/notifications.service';
import { readPage, resolvePage } from '../../../core/utils/read-list';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-notifications',
  standalone: true,
  imports: [PaginationComponent, IconComponent, NotificationCardComponent, EmptyStateComponent, TranslatePipe],
  template: `
    <h1 class="page-title">{{ 'nav.notifications' | t }}</h1>
    <p class="muted">{{ 'notifications.subtitle' | t }}</p>
    @if (markFailed()) { <p class="error-state" role="alert">{{ 'notifications.markFailed' | t }}</p> }
    @if (loading()) { <p class="muted">{{ 'loading' | t }}</p> }
    @else if (failed()) { <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost icon-action" type="button" (click)="load()" [attr.aria-label]="'actions.retry' | t" [title]="'actions.retry' | t"><app-icon name="retry" /></button></div> } @else if (!items().length) { <app-empty-state messageKey="empty.notifications" icon="bell" /> } @else {
      <div class="stack">
        @for (n of items(); track n.id) {
          <app-notification-card [notification]="n" portal="admin" [marking]="markingIds().includes(n.id)" (read)="mark($event)" />
        }
      </div>
    }
    <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" [disabled]="loading()" labelKey="pagination.notifications" (change)="load($event)" />
  `,
})
export class AdminNotificationsComponent implements OnInit {
  private readonly notifications = inject(NotificationsService);
  readonly items = signal<NotificationDto[]>([]);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly markingIds = signal<string[]>([]);
  readonly markFailed = signal(false);
  page = 1; totalPages = 1; totalCount = 0;
  private readonly pageRequest = new PageRequest(inject(DestroyRef));

  ngOnInit(): void { this.load(1); }
  load(page = this.page): void {
    this.page = page; this.loading.set(true); this.failed.set(false);
    this.pageRequest.run(this.notifications.list('admin', { pageNumber: page, pageSize: 20 }), {
      next: response => {
        const result = readPage<NotificationDto>(response);
        const targetPage = resolvePage(page, result);
        if (page !== targetPage) { this.load(targetPage); return; }
        this.items.set(result.items); this.totalPages = result.totalPages; this.totalCount = result.totalCount;
        this.loading.set(false);
      },
      error: () => { this.failed.set(true); this.loading.set(false); },
    });
  }
  mark(n: NotificationDto): void {
    if (n.isRead || this.markingIds().includes(n.id)) return;
    this.markFailed.set(false);
    this.markingIds.update(ids => [...ids, n.id]);
    this.notifications.markRead('admin', n.id).subscribe({
      next: () => {
        this.items.update(list => list.map(item => item.id === n.id ? { ...item, isRead: true } : item));
        this.markingIds.update(ids => ids.filter(id => id !== n.id));
      },
      error: () => { this.markingIds.update(ids => ids.filter(id => id !== n.id)); this.markFailed.set(true); },
    });
  }
}
