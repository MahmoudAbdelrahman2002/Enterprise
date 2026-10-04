import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { I18nService } from '../../../core/services/i18n.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { AdminProviderDetailComponent } from './admin-provider-detail.component';
import { AdminProvidersComponent } from './admin-providers.component';

describe('Admin provider auxiliary permissions', () => {
  const provider = {
    id: 'provider', userId: 'user', email: 'provider@example.com',
    firstName: 'Demo', lastName: 'Provider', companyName: 'Demo store',
    phoneNumber: '+201000000030', isActive: true,
    serviceId: '11111111-1111-1111-1111-111111111111', serviceName: 'Restaurant',
    createdAtUtc: '2026-10-03T00:00:00Z',
  };
  let permissions: string[];
  let http: HttpTestingController;
  let toast: jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    permissions = ['Providers.Read'];
    toast = jasmine.createSpyObj<ToastService>('ToastService', ['error', 'success']);
    await TestBed.configureTestingModule({
      imports: [AdminProvidersComponent, AdminProviderDetailComponent],
      providers: [
        provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: provider.id }) } } },
        { provide: TokenStoreService, useValue: { hasPermission: (_portal: string, permission: string) => permissions.includes(permission) } },
        { provide: I18nService, useValue: { lang: () => 'en', t: (key: string) => key } },
        { provide: ToastService, useValue: toast },
      ],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  function flushList(): void {
    const request = http.expectOne(req => req.url === '/api/v1/admin/providers');
    expect(request.request.method).toBe('GET');
    request.flush({ success: true, data: { items: [provider], totalPages: 1, totalCount: 1 } });
    http.expectNone('/api/v1/admin/services/lookup');
  }

  it('loads the Providers.Read list without a service request or error on navigation and reload', () => {
    for (let reload = 0; reload < 2; reload++) {
      const fixture = TestBed.createComponent(AdminProvidersComponent);
      fixture.detectChanges();
      flushList();
      fixture.detectChanges();
      expect(fixture.nativeElement.textContent).toContain('Demo store');
      expect(fixture.nativeElement.textContent).toContain('Restaurant');
      expect(fixture.componentInstance.failed()).toBeFalse();
      expect(toast.error).not.toHaveBeenCalled();
      fixture.destroy();
    }
  });

  it('does not load service choices when the list has no create operation', () => {
    permissions.push('Services.Read');
    const fixture = TestBed.createComponent(AdminProvidersComponent);
    fixture.detectChanges();
    flushList();
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('does not request or offer unauthorized service choices to a provider creator', () => {
    permissions.push('Providers.Create');
    const fixture = TestBed.createComponent(AdminProvidersComponent);
    fixture.detectChanges();
    flushList();
    fixture.componentInstance.showForm.set(true);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('form')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('#admin-providers-serviceId')).toBeNull();
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('keeps service selection available to an authorized provider creator', () => {
    permissions.push('Providers.Create', 'Services.Read');
    const fixture = TestBed.createComponent(AdminProvidersComponent);
    fixture.detectChanges();
    http.expectOne('/api/v1/admin/services/lookup').flush({ success: true, data: [{ id: provider.serviceId, name: 'Restaurant' }] });
    flushList();
    fixture.componentInstance.showForm.set(true);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('#admin-providers-serviceId').textContent).toContain('Restaurant');
  });

  it('shows the existing service on a read-only detail view without fetching choices', () => {
    const fixture = TestBed.createComponent(AdminProviderDetailComponent);
    fixture.detectChanges();
    http.expectOne('/api/v1/admin/providers/provider').flush({ success: true, data: provider });
    http.expectNone('/api/v1/admin/services/lookup');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Restaurant');
    expect(fixture.componentInstance.form.disabled).toBeTrue();
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('preserves the assigned service when an editor cannot read service choices', () => {
    permissions.push('Providers.Update');
    const fixture = TestBed.createComponent(AdminProviderDetailComponent);
    fixture.detectChanges();
    http.expectOne('/api/v1/admin/providers/provider').flush({ success: true, data: provider });
    http.expectNone('/api/v1/admin/services/lookup');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Restaurant');
    expect(fixture.nativeElement.querySelector('#admin-provider-detail-serviceId')).toBeNull();
    fixture.componentInstance.form.controls.companyName.setValue('Updated store');
    fixture.componentInstance.save();
    const update = http.expectOne('/api/v1/admin/providers/provider');
    expect(update.request.method).toBe('PUT');
    expect(update.request.body.serviceId).toBe(provider.serviceId);
    update.flush({ success: true, data: { ...provider, companyName: 'Updated store' } });
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('loads service choices for an editor with Services.Read', () => {
    permissions.push('Providers.Update', 'Services.Read');
    const fixture = TestBed.createComponent(AdminProviderDetailComponent);
    fixture.detectChanges();
    http.expectOne('/api/v1/admin/services/lookup').flush({ success: true, data: [{ id: provider.serviceId, name: 'Restaurant' }] });
    http.expectOne('/api/v1/admin/providers/provider').flush({ success: true, data: provider });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('#admin-provider-detail-serviceId').value).toBe(provider.serviceId);
  });
});
