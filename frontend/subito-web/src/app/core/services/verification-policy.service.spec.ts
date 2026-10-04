import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ApiService } from './api.service';
import { VerificationPolicyService } from './verification-policy.service';

describe('Verification guidance policy', () => {
  it('shares configured expiry guidance across code-entry screens', () => {
    const policy = { codeLength: 6, expirationMinutes: 7, maxAttempts: 4, resendWaitSeconds: 30 };
    const get = jasmine.createSpy().and.returnValue(of(policy));
    TestBed.configureTestingModule({ providers: [{ provide: ApiService, useValue: { get } }] });
    const service = TestBed.inject(VerificationPolicyService);
    const values: unknown[] = [];
    service.policy$.subscribe(value => values.push(value)); service.policy$.subscribe(value => values.push(value));
    expect(get).toHaveBeenCalledTimes(1); expect(values).toEqual([policy, policy]);
  });
  it('keeps recovery available when optional policy retrieval fails', () => {
    TestBed.configureTestingModule({ providers: [{ provide: ApiService, useValue: { get: () => throwError(() => new Error('offline')) } }] });
    let result: unknown = 'unset';
    TestBed.inject(VerificationPolicyService).policy$.subscribe(value => result = value);
    expect(result).toBeNull();
  });
});
