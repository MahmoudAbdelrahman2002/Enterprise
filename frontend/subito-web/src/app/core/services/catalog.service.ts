import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CategoryDetailDto,
  ClientCategoryDto,
  ClientProductDto,
  ClientProviderListItemDto,
  MarketplaceServiceDto,
  MarketplaceServiceLookupDto,
  PagedResult,
  ProductDetailDto,
  ProductListItemDto,
} from '../models/domain.models';
import { ApiService, Query } from './api.service';

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly api = inject(ApiService);

  listClientServices(pageSize = 50, searchTerm?: string | null): Observable<unknown> {
    return this.api.get('/client/services', { pageSize, searchTerm: searchTerm || null });
  }

  listProvidersByService(serviceId: string, query: Query): Observable<PagedResult<ClientProviderListItemDto>> {
    return this.api.get(`/client/services/${serviceId}/providers`, query);
  }

  getClientProvider(providerId: string): Observable<ClientProviderListItemDto> {
    return this.api.get(`/client/providers/${providerId}`);
  }

  listStoreCategories(providerId: string, pageSize = 100): Observable<ClientCategoryDto[] | { items?: ClientCategoryDto[] }> {
    return this.api.get(`/client/${providerId}/categories`, { pageSize });
  }

  listStoreProducts(
    providerId: string,
    query: Query
  ): Observable<ClientProductDto[] | { items?: ClientProductDto[] }> {
    return this.api.get(`/client/${providerId}/products`, query);
  }

  getClientProduct(id: string): Observable<ClientProductDto> {
    return this.api.get(`/client/products/${id}`);
  }

  listAdminServices(query: Query): Observable<PagedResult<MarketplaceServiceDto>> {
    return this.api.get('/admin/services', query);
  }

  lookupAdminServices(): Observable<MarketplaceServiceLookupDto[] | { items?: MarketplaceServiceLookupDto[] }> {
    return this.api.get('/admin/services/lookup');
  }

  createAdminService(body: unknown): Observable<unknown> {
    return this.api.post('/admin/services', body);
  }

  setAdminServiceActive(id: string, isActive: boolean): Observable<unknown> {
    return this.api.post(`/admin/services/${id}/set-active`, { isActive });
  }

  deleteAdminService(id: string): Observable<unknown> {
    return this.api.delete(`/admin/services/${id}`);
  }

  uploadAdminServiceImage(id: string, file: File): Observable<unknown> {
    return this.api.upload(`/admin/services/${id}/image`, file);
  }

  listProviderCategories(query: Query): Observable<CategoryDetailDto[] | PagedResult<CategoryDetailDto>> {
    return this.api.get('/provider/categories', query);
  }

  lookupProviderCategories(): Observable<CategoryDetailDto[] | PagedResult<CategoryDetailDto>> {
    return this.api.get('/provider/categories/lookup');
  }

  createProviderCategory(body: unknown): Observable<unknown> {
    return this.api.post('/provider/categories', body);
  }

  setProviderCategoryActive(id: string, isActive: boolean): Observable<unknown> {
    return this.api.post(`/provider/categories/${id}/set-active`, { isActive });
  }

  deleteProviderCategory(id: string): Observable<unknown> {
    return this.api.delete(`/provider/categories/${id}`);
  }

  uploadProviderCategoryImage(id: string, file: File): Observable<unknown> {
    return this.api.upload(`/provider/categories/${id}/image`, file);
  }

  listProviderProducts(query: Query): Observable<PagedResult<ProductListItemDto>> {
    return this.api.get('/provider/products', query);
  }

  createProviderProduct(categoryId: string, body: unknown): Observable<ProductDetailDto> {
    return this.api.post(`/provider/categories/${categoryId}/products`, body);
  }

  deleteProviderProduct(categoryId: string, productId: string): Observable<unknown> {
    return this.api.delete(`/provider/categories/${categoryId}/products/${productId}`);
  }

  uploadProviderProductImage(categoryId: string, productId: string, file: File): Observable<unknown> {
    return this.api.upload(`/provider/categories/${categoryId}/products/${productId}/image`, file);
  }
}
