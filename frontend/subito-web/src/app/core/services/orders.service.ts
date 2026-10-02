import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { OrderDetailDto, OrderListItemDto, PagedResult } from '../models/domain.models';
import { ApiService, Query } from './api.service';

@Injectable({ providedIn: 'root' })
export class OrdersService {
  private readonly api = inject(ApiService);

  listClient(query: Query): Observable<OrderListItemDto[] | PagedResult<OrderListItemDto>> {
    return this.api.get('/client/orders', query);
  }

  getClient(id: string): Observable<OrderDetailDto> {
    return this.api.get(`/client/orders/${id}`);
  }

  getBySession(sessionId: string): Observable<OrderDetailDto> {
    return this.api.get(`/client/orders/by-session/${sessionId}`, undefined, { silent: true });
  }

  confirmSession(sessionId: string): Observable<OrderDetailDto> {
    return this.api.post(`/client/orders/confirm-session/${sessionId}`);
  }

  cancelClient(id: string): Observable<unknown> {
    return this.api.post(`/client/orders/${id}/cancel`);
  }

  listProvider(query: Query): Observable<PagedResult<OrderListItemDto>> {
    return this.api.get('/provider/orders', query);
  }

  getProvider(id: string): Observable<OrderDetailDto> {
    return this.api.get(`/provider/orders/${id}`);
  }

  updateProviderStatus(id: string, status: number): Observable<unknown> {
    return this.api.patch(`/provider/orders/${id}/status`, { status });
  }

  listAdmin(query: Query): Observable<PagedResult<OrderListItemDto>> {
    return this.api.get('/admin/orders', query);
  }

  getAdmin(id: string): Observable<OrderDetailDto> {
    return this.api.get(`/admin/orders/${id}`);
  }
}
