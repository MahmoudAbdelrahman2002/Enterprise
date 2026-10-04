import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { NotificationDto, PagedResult, UnreadNotificationCountDto } from '../models/domain.models';
import { Portal } from '../models/api.models';
import { ApiService, Query } from './api.service';

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private readonly api = inject(ApiService);

  list(portal: Portal, query: Query = {}): Observable<PagedResult<NotificationDto>> {
    return this.api.get(`/${portal}/notifications/paged`, query);
  }

  markRead(portal: Portal, id: string): Observable<unknown> {
    return this.api.post(`/${portal}/notifications/${id}/read`);
  }

  unreadCount(): Observable<UnreadNotificationCountDto> {
    return this.api.get('/client/notifications/count');
  }
}
