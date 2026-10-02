import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { NotificationDto, PagedResult, UnreadNotificationCountDto } from '../models/domain.models';
import { Portal } from '../models/api.models';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private readonly api = inject(ApiService);

  list(portal: Portal): Observable<PagedResult<NotificationDto> | NotificationDto[]> {
    return this.api.get(`/${portal}/notifications`);
  }

  markRead(portal: Portal, id: string): Observable<unknown> {
    return this.api.post(`/${portal}/notifications/${id}/read`);
  }

  unreadCount(): Observable<UnreadNotificationCountDto> {
    return this.api.get('/client/notifications/count');
  }
}
