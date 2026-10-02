import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { DashboardLayoutComponent, NavItem } from '../../../layouts/dashboard-layout/dashboard-layout.component';

@Component({
  selector: 'app-provider-shell',
  standalone: true,
  imports: [TranslatePipe, DashboardLayoutComponent, RouterOutlet],
  template: `
    <app-dashboard-layout portal="provider" [title]="'nav.provider' | t" homeLink="/provider" [items]="items">
      <router-outlet />
    </app-dashboard-layout>
  `,
})
export class ProviderShellComponent {
  items: NavItem[] = [
    { labelKey: 'nav.dashboard', link: '/provider' },
    { labelKey: 'nav.store', link: '/provider/store' },
    { labelKey: 'nav.categories', link: '/provider/categories', permissions: ['ProviderCategory.Read'] },
    { labelKey: 'nav.products', link: '/provider/products', permissions: ['ProviderProduct.Read'] },
    { labelKey: 'nav.orders', link: '/provider/orders', permissions: ['ProviderOrder.Read'] },
    { labelKey: 'nav.roles', link: '/provider/roles', permissions: ['ProviderRoles.Read'] },
    { labelKey: 'nav.staff', link: '/provider/staff', permissions: ['ProviderStaff.Read'] },
    { labelKey: 'nav.notifications', link: '/provider/notifications' },
    { labelKey: 'nav.profile', link: '/provider/profile' },
  ];
}
