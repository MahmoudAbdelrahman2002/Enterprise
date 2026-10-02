import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResult, StaffDetailDto, StaffListItemDto } from '../models/domain.models';
import { ApiService, Query } from './api.service';

@Injectable({ providedIn: 'root' })
export class StaffService {
  private readonly api = inject(ApiService);

  listAdmin(query: Query): Observable<PagedResult<StaffListItemDto>> {
    return this.api.get('/admin/users', query);
  }

  createAdmin(body: unknown): Observable<StaffDetailDto> {
    return this.api.post('/admin/users', body);
  }

  setAdminActive(id: string, isActive: boolean): Observable<unknown> {
    return this.api.post(`/admin/users/${id}/set-active`, { isActive });
  }

  deleteAdmin(id: string): Observable<unknown> {
    return this.api.delete(`/admin/users/${id}`);
  }

  listProvider(query: Query): Observable<PagedResult<StaffListItemDto>> {
    return this.api.get('/provider/staff', query);
  }

  createProvider(body: unknown): Observable<StaffDetailDto> {
    return this.api.post('/provider/staff', body);
  }

  setProviderActive(id: string, isActive: boolean): Observable<unknown> {
    return this.api.post(`/provider/staff/${id}/set-active`, { isActive });
  }

  deleteProvider(id: string): Observable<unknown> {
    return this.api.delete(`/provider/staff/${id}`);
  }
}
