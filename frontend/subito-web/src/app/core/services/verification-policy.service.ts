import { Injectable, inject } from '@angular/core';
import { catchError, of, shareReplay } from 'rxjs';
import { ApiService } from './api.service';

export interface VerificationPolicy { codeLength: number; expirationMinutes: number; maxAttempts: number; resendWaitSeconds: number; }

@Injectable({ providedIn: 'root' })
export class VerificationPolicyService {
  readonly policy$ = inject(ApiService).get<VerificationPolicy>('/auth/verification-policy', undefined, { silent: true })
    .pipe(catchError(() => of(null)), shareReplay({ bufferSize: 1, refCount: false }));
}
