import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AdminClientDto,
  CategoryDetailDto,
  PagedResult,
  ProductListItemDto,
  ProviderAdminDto,
} from '../models/domain.models';
import { ApiService, Query } from './api.service';

@Injectable({ providedIn: 'root' })
export class ProvidersService {
  private readonly api = inject(ApiService);

  list(query: Query): Observable<PagedResult<ProviderAdminDto>> {
    return this.api.get('/admin/providers', query);
  }

  get(id: string): Observable<ProviderAdminDto> {
    return this.api.get(`/admin/providers/${id}`);
  }

  create(body: unknown): Observable<unknown> {
    return this.api.post('/admin/providers', body);
  }

  update(id: string, body: unknown): Observable<ProviderAdminDto> {
    return this.api.put(`/admin/providers/${id}`, body);
  }

  setActive(id: string, isActive: boolean): Observable<unknown> {
    return this.api.post(`/admin/providers/${id}/set-active`, { isActive });
  }

  remove(id: string): Observable<unknown> {
    return this.api.delete(`/admin/providers/${id}`);
  }

  uploadImage(id: string, file: File): Observable<ProviderAdminDto> {
    return this.api.upload(`/admin/providers/${id}/image`, file);
  }

  listCategories(id: string): Observable<CategoryDetailDto[]> {
    return this.api.get(`/admin/providers/${id}/categories`);
  }

  listProducts(id: string): Observable<ProductListItemDto[]> {
    return this.api.get(`/admin/providers/${id}/products`);
  }

  listClients(query: Query): Observable<PagedResult<AdminClientDto>> {
    return this.api.get('/admin/clients', query);
  }

  setClientActive(id: string, isActive: boolean): Observable<unknown> {
    return this.api.post(`/admin/clients/${id}/set-active`, { isActive });
  }
}
