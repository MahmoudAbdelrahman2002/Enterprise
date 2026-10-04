import { Type, signal } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { ConfirmService } from '../../../core/services/confirm.service';
import { I18nService } from '../../../core/services/i18n.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { ToastService } from '../../../core/services/toast.service';
import { ProviderProductsComponent } from '../../../features/provider/products/provider-products.component';
import { ProviderCategoriesComponent } from '../../../features/provider/categories/provider-categories.component';
import { ProviderRolesComponent } from '../../../features/provider/roles/provider-roles.component';
import { ProviderStaffComponent } from '../../../features/provider/staff/provider-staff.component';
import { OrdersListComponent } from '../../../features/client/orders/orders-list.component';

describe('Empty-state setup actions', () => {
  let permissions: Set<string>;
  let fixture: ComponentFixture<unknown>;

  beforeEach(async () => {
    permissions = new Set();
    await TestBed.configureTestingModule({
      imports: [ProviderProductsComponent, ProviderCategoriesComponent, ProviderRolesComponent, ProviderStaffComponent, OrdersListComponent],
      providers: [
        provideRouter([]),
        { provide: ApiService, useValue: { get: () => of([]) } },
        { provide: ConfirmService, useValue: {} },
        { provide: ToastService, useValue: {} },
        { provide: TokenStoreService, useValue: { hasPermission: (_portal: string, permission: string) => permissions.has(permission) } },
        { provide: I18nService, useValue: { lang: signal('en'), t: (key: string) => key } },
      ],
    }).compileComponents();
  });

  function render<T>(component: Type<T>): ComponentFixture<T> {
    const result = TestBed.createComponent(component);
    fixture = result;
    result.detectChanges();
    return result;
  }

  afterEach(() => fixture?.destroy());

  it('takes a client with no orders to browsing', () => {
    const page = render(OrdersListComponent).nativeElement as HTMLElement;
    const link = page.querySelector<HTMLAnchorElement>('app-empty-state a')!;
    expect(link.getAttribute('href')).toBe('/');
    expect(link.textContent).toContain('home.browseStores');
  });

  it('offers category setup before product creation when categories are missing', () => {
    permissions = new Set(['ProviderProduct.Create', 'ProviderCategory.Read', 'ProviderCategory.Create']);
    const page = render(ProviderProductsComponent).nativeElement as HTMLElement;
    expect(page.querySelector('app-empty-state')!.textContent).toContain('empty.productsNeedCategory');
    expect(page.querySelector('app-empty-state a')!.getAttribute('href')).toBe('/provider/categories');
    expect(page.querySelector('app-empty-state button')).toBeNull();
  });

  it('opens product creation when a category is available', () => {
    permissions.add('ProviderProduct.Create');
    const result = render(ProviderProductsComponent);
    result.componentInstance.categories.set([{
      id: 'category', providerId: 'provider', name: 'Category',
      displayOrder: 0, isActive: true, imageUrl: null,
    }]);
    result.detectChanges();
    result.nativeElement.querySelector('app-empty-state button').click();
    result.detectChanges();
    expect(result.componentInstance.showForm()).toBeTrue();
    expect(result.nativeElement.querySelector('#provider-products-categoryId')).not.toBeNull();
  });

  it('clears an empty product search without starting catalogue setup', fakeAsync(() => {
    permissions.add('ProviderProduct.Create');
    const result = render(ProviderProductsComponent);
    result.componentInstance.search.setValue('missing product');
    tick(300); result.detectChanges();
    expect(result.nativeElement.querySelector('app-empty-state').textContent).toContain('empty.productsSearch');
    const button = result.nativeElement.querySelector('app-empty-state button') as HTMLButtonElement;
    expect(button.textContent).toContain('actions.clear');
    button.click(); tick(300); result.detectChanges();
    expect(result.componentInstance.search.value).toBe('');
    expect(result.componentInstance.showForm()).toBeFalse();
  }));

  it('offers role setup before staff creation when roles are missing', () => {
    permissions = new Set(['ProviderStaff.Create', 'ProviderRoles.Read', 'ProviderRoles.Create']);
    const page = render(ProviderStaffComponent).nativeElement as HTMLElement;
    expect(page.querySelector('app-empty-state')!.textContent).toContain('empty.staffNeedRole');
    expect(page.querySelector('app-empty-state a')!.getAttribute('href')).toBe('/provider/roles');
  });

  it('opens staff creation when a role is available', () => {
    permissions.add('ProviderStaff.Create');
    const result = render(ProviderStaffComponent);
    result.componentInstance.roles.set([{
      id: 'role', name: 'Staff', roleType: 'Provider', providerId: null, isSystem: false, usersCount: 0, permissions: [],
    }]);
    result.detectChanges();
    result.nativeElement.querySelector('app-empty-state button').click();
    result.detectChanges();
    expect(result.componentInstance.showForm()).toBeTrue();
    expect(result.nativeElement.querySelector('#provider-staff-roleId')).not.toBeNull();
  });

  for (const { type, permission } of [
    { type: ProviderCategoriesComponent as Type<unknown>, permission: 'ProviderCategory.Create' },
    { type: ProviderRolesComponent as Type<unknown>, permission: 'ProviderRoles.Create' },
  ]) {
    it(`opens the first setup form with ${permission}`, () => {
      permissions.add(permission);
      const page = render(type).nativeElement as HTMLElement;
      page.querySelector<HTMLButtonElement>('app-empty-state button')!.click();
      fixture.detectChanges();
      expect(page.querySelector('form')).not.toBeNull();
    });
  }

  for (const type of [ProviderProductsComponent, ProviderCategoriesComponent, ProviderRolesComponent, ProviderStaffComponent] as Type<unknown>[]) {
    it(`hides creation and setup links from readers on ${type.name}`, () => {
      const page = render(type).nativeElement as HTMLElement;
      expect(page.querySelector('app-empty-state button')).toBeNull();
      expect(page.querySelector('app-empty-state a')).toBeNull();
    });
  }

  it('hides category setup when product creators lack category permissions', () => {
    permissions.add('ProviderProduct.Create');
    const page = render(ProviderProductsComponent).nativeElement as HTMLElement;
    expect(page.querySelector('app-empty-state a')).toBeNull();
  });

  it('hides role setup when staff creators lack role permissions', () => {
    permissions.add('ProviderStaff.Create');
    const page = render(ProviderStaffComponent).nativeElement as HTMLElement;
    expect(page.querySelector('app-empty-state a')).toBeNull();
  });
});
