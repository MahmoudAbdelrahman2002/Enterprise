import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CategoryDetailDto,
  ClientCategoryDto,
  ClientMarketServiceDto,
  ClientProductDto,
  ClientProviderListItemDto,
  MarketplaceServiceDto,
  MarketplaceServiceLookupDto,
  PagedResult,
  ProductDetailDto,
  ProductListItemDto,
} from '../models/domain.models';
import { ApiService, Query, RequestOptions } from './api.service';

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly api = inject(ApiService);

  listClientServices(pageSize = 12, searchTerm?: string | null, pageNumber = 1): Observable<PagedResult<ClientMarketServiceDto>> {
    return this.api.get('/client/services', { pageNumber, pageSize, searchTerm: searchTerm || null });
  }

  listProvidersByService(serviceId: string, query: Query): Observable<PagedResult<ClientProviderListItemDto>> {
    return this.api.get(`/client/services/${serviceId}/providers`, query);
  }

  getClientProvider(providerId: string, options?: RequestOptions): Observable<ClientProviderListItemDto> {
    return this.api.get(`/client/providers/${providerId}`, undefined, options);
  }

  listStoreCategories(providerId: string, pageSize = 12, options?: RequestOptions, pageNumber = 1): Observable<PagedResult<ClientCategoryDto>> {
    return this.api.get(`/client/${providerId}/categories`, { pageNumber, pageSize }, options);
  }

  listStoreProducts(
    providerId: string,
    query: Query, options?: RequestOptions
  ): Observable<ClientProductDto[] | { items?: ClientProductDto[] }> {
    return this.api.get(`/client/${providerId}/products`, query, options);
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

  getAdminService(id: string): Observable<MarketplaceServiceDto> {
    return this.api.get(`/admin/services/${id}`);
  }

  updateAdminService(id: string, body: unknown): Observable<MarketplaceServiceDto> {
    return this.api.put(`/admin/services/${id}`, body);
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

  getProviderCategory(id: string): Observable<CategoryDetailDto> {
    return this.api.get(`/provider/categories/${id}`);
  }

  updateProviderCategory(id: string, body: unknown): Observable<CategoryDetailDto> {
    return this.api.put(`/provider/categories/${id}`, body);
  }

  setProviderCategoryActive(id: string, isActive: boolean): Observable<unknown> {
    return this.api.post(`/provider/categories/${id}/set-active`, { isActive });
  }

  deleteProviderCategory(id: string): Observable<unknown> {
    return this.api.delete(`/provider/categories/${id}`, { silent: true });
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

  getProviderProduct(categoryId: string, productId: string): Observable<ProductDetailDto> {
    return this.api.get(`/provider/categories/${categoryId}/products/${productId}`);
  }

  updateProviderProduct(categoryId: string, productId: string, body: unknown): Observable<ProductDetailDto> {
    return this.api.put(`/provider/categories/${categoryId}/products/${productId}`, body);
  }

  deleteProviderProduct(categoryId: string, productId: string): Observable<unknown> {
    return this.api.delete(`/provider/categories/${categoryId}/products/${productId}`);
  }

  uploadProviderProductImage(categoryId: string, productId: string, file: File): Observable<unknown> {
    return this.api.upload(`/provider/categories/${categoryId}/products/${productId}/image`, file);
  }
}
