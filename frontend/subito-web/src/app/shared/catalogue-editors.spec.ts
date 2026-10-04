import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { I18nService } from '../core/services/i18n.service';
import { TokenStoreService } from '../core/services/token-store.service';
import { ToastService } from '../core/services/toast.service';
import { AdminServicesComponent } from '../features/admin/services/admin-services.component';
import { ProviderCategoriesComponent } from '../features/provider/categories/provider-categories.component';
import { ProviderProductsComponent } from '../features/provider/products/provider-products.component';

describe('Catalogue record editors', () => {
  let http: HttpTestingController;
  let allowUpdates: boolean;
  const categoryId = '11111111-1111-4111-8111-111111111111';
  const destinationId = '22222222-2222-4222-8222-222222222222';
  const translations = {
    name: { en: 'English name', ar: 'اسم', it: 'Nome' },
    description: { en: 'English description', ar: 'وصف', it: 'Descrizione' },
  };
  const record = { id: 'record', providerId: 'provider', categoryId, name: 'Nome',
    code: 'SERVICE', sku: 'PRODUCT', price: 12.5, status: 1, displayOrder: 2, isActive: true, translations };
  const scenarios = [
    { component: ProviderProductsComponent, list: '/provider/products', detail: `/provider/categories/${categoryId}/products/record`, product: true },
    { component: ProviderCategoriesComponent, list: '/provider/categories', detail: '/provider/categories/record', product: false },
    { component: AdminServicesComponent, list: '/admin/services', detail: '/admin/services/record', product: false },
  ];

  beforeEach(async () => {
    allowUpdates = true;
    await TestBed.configureTestingModule({
      imports: [ProviderProductsComponent, ProviderCategoriesComponent, AdminServicesComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        { provide: TokenStoreService, useValue: { hasPermission: (_role: string, permission: string) => !permission.endsWith('.Update') || allowUpdates } },
        { provide: ToastService, useValue: { success: jasmine.createSpy(), error: jasmine.createSpy() } },
        { provide: I18nService, useValue: { lang: signal('it'), t: (key: string) => key } },
      ],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  for (const scenario of scenarios) {
    function setup() {
      const fixture = TestBed.createComponent<any>(scenario.component);
      fixture.detectChanges();
      if (scenario.product) http.expectOne('/api/v1/provider/categories/lookup').flush([{ id: categoryId, name: 'Category' }, { id: destinationId, name: 'New category' }]);
      http.expectOne(request => request.url === '/api/v1' + scenario.list).flush([record]);
      fixture.detectChanges();
      return fixture;
    }

    it(`updates ${scenario.list} in place with all saved translations`, () => {
      const fixture = setup();
      const component = fixture.componentInstance;
      fixture.nativeElement.querySelector('button[id^="edit-"]').click();
      http.expectOne('/api/v1' + scenario.detail).flush(record);
      fixture.detectChanges();
      expect(component.form.controls.nameEn.value).toBe('English name');
      expect(component.form.controls.nameAr.value).toBe('اسم');
      component.form.controls.nameEn.setValue('Updated name');
      if (scenario.product) component.form.patchValue({ categoryId: destinationId, price: 15, status: 2 });
      component.create();
      const request = http.expectOne('/api/v1' + scenario.detail);
      expect(request.request.method).toBe('PUT');
      expect(request.request.body.name).toEqual({ ...translations.name, en: 'Updated name' });
      expect(request.request.body.description).toEqual(translations.description);
      if (scenario.product) {
        expect(request.request.body.categoryId).toBe(destinationId);
        expect(request.request.body.price).toBe(15);
        expect(request.request.body.status).toBe(2);
      }
      request.flush({ ...record, name: 'Updated name' });
      fixture.detectChanges();
      http.expectOne(request => request.url === '/api/v1' + scenario.list).flush([{ ...record, name: 'Updated name' }]);
      fixture.detectChanges();
      expect(component.editing()).toBeNull();
      expect(component.showForm()).toBeFalse();
      expect(fixture.nativeElement.textContent).toContain('Updated name');
      expect(document.activeElement?.id).toMatch(/^edit-/);
    });

    it(`cancels ${scenario.list} edits without saving and resets the create form`, () => {
      const fixture = setup();
      const component = fixture.componentInstance;
      component.edit(record); http.expectOne('/api/v1' + scenario.detail).flush(record);
      fixture.detectChanges();
      component.form.controls.nameEn.setValue('Discard'); component.cancelEdit(); fixture.detectChanges();
      http.expectNone(request => request.method === 'PUT');
      expect(document.activeElement?.id).toMatch(/^edit-/);
      component.startCreate(); fixture.detectChanges();
      expect(component.editing()).toBeNull();
      expect(component.form.controls.nameEn.value).toBe('');
    });

    it(`hides ${scenario.list} edit controls and refuses direct editing without update permission`, () => {
      allowUpdates = false;
      const fixture = setup();
      expect(fixture.nativeElement.querySelector('button[id^="edit-"]')).toBeNull();
      fixture.componentInstance.edit(record);
      http.expectNone('/api/v1' + scenario.detail);
      expect(fixture.componentInstance.showForm()).toBeFalse();
    });
  }

  it('keeps entered product changes on update failure and prevents duplicate submission', () => {
    const fixture = TestBed.createComponent(ProviderProductsComponent);
    fixture.detectChanges();
    http.expectOne('/api/v1/provider/categories/lookup').flush([{ id: categoryId, name: 'Category' }]);
    http.expectOne(request => request.url === '/api/v1/provider/products').flush([record]);
    const component = fixture.componentInstance;
    component.edit(record); http.expectOne(`/api/v1/provider/categories/${categoryId}/products/record`).flush(record);
    fixture.detectChanges();
    component.form.controls.price.setValue(25); component.create(); component.create();
    http.expectOne(`/api/v1/provider/categories/${categoryId}/products/record`).flush({ message: 'SKU already exists' }, { status: 409, statusText: 'Conflict' });
    expect(component.busy()).toBeFalse();
    expect(component.showForm()).toBeTrue();
    expect(component.form.controls.price.value).toBe(25);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('form').textContent).toContain('SKU already exists');
  });
});
