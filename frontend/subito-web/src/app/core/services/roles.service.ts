import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResult, PermissionGroupDto, RoleDetailDto, RoleListItemDto } from '../models/domain.models';
import { ApiService, Query } from './api.service';
import { readAllPages } from '../utils/read-all-pages';

export type RolePortal = 'admin' | 'provider';

@Injectable({ providedIn: 'root' })
export class RolesService {
  private readonly api = inject(ApiService);

  permissions(portal: RolePortal): Observable<PermissionGroupDto[]> {
    return this.api.get(`/${portal}/permissions`);
  }

  list(portal: RolePortal, query: Query): Observable<PagedResult<RoleListItemDto>> {
    return this.api.get(`/${portal}/roles`, query);
  }

  lookup(portal: RolePortal): Observable<RoleListItemDto[]> {
    return readAllPages<RoleListItemDto>(page => this.list(portal, { pageNumber: page, pageSize: 100 }));
  }

  get(portal: RolePortal, id: string): Observable<RoleDetailDto> {
    return this.api.get(`/${portal}/roles/${id}`);
  }

  create(portal: RolePortal, body: unknown): Observable<unknown> {
    return this.api.post(`/${portal}/roles`, body);
  }

  update(portal: RolePortal, id: string, body: unknown): Observable<unknown> {
    return this.api.put(`/${portal}/roles/${id}`, body);
  }

  remove(portal: RolePortal, id: string): Observable<unknown> {
    return this.api.delete(`/${portal}/roles/${id}`);
  }
}
