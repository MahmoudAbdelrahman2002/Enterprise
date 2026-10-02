import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ProviderStoreDto } from '../models/domain.models';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class StoreService {
  private readonly api = inject(ApiService);

  get(): Observable<ProviderStoreDto> {
    return this.api.get('/provider/store');
  }

  update(companyName: string, phoneNumber: string): Observable<ProviderStoreDto> {
    return this.api.put('/provider/store', { companyName, phoneNumber });
  }

  uploadImage(file: File): Observable<unknown> {
    return this.api.upload('/provider/profile/image', file);
  }
}
