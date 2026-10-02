import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { DashboardLayoutComponent, NavItem } from '../../../layouts/dashboard-layout/dashboard-layout.component';

@Component({
  selector: 'app-admin-shell',
  standalone: true,
  imports: [TranslatePipe, DashboardLayoutComponent, RouterOutlet],
  template: `
    <app-dashboard-layout portal="admin" [title]="'nav.admin' | t" homeLink="/admin" [items]="items">
      <router-outlet />
    </app-dashboard-layout>
  `,
})
export class AdminShellComponent {
  items: NavItem[] = [
    { labelKey: 'nav.dashboard', link: '/admin' },
    { labelKey: 'nav.providers', link: '/admin/providers', permissions: ['Providers.Read'] },
    { labelKey: 'nav.services', link: '/admin/services', permissions: ['Services.Read'] },
    { labelKey: 'nav.clients', link: '/admin/clients', permissions: ['Clients.Read'] },
    { labelKey: 'nav.roles', link: '/admin/roles', permissions: ['Roles.Read'] },
    { labelKey: 'nav.users', link: '/admin/users', permissions: ['Admins.Read'] },
    { labelKey: 'nav.notifications', link: '/admin/notifications' },
    { labelKey: 'nav.profile', link: '/admin/profile' },
  ];
}
