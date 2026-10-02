const fs = require('fs');
const path = require('path');
const root = path.join('frontend', 'subito-web', 'src', 'app', 'features');
const write = (rel, content) => {
  const p = path.join(root, rel);
  fs.mkdirSync(path.dirname(p), { recursive: true });
  fs.writeFileSync(p, content);
  console.log(rel);
};

const passwordAuth = (portal, selector, className, home) => `import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { LogoComponent } from '../../../shared/components/logo/logo.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: '${selector}',
  standalone: true,
  imports: [FormsModule, RouterLink, LogoComponent, TranslatePipe],
  template: \`
    <div class="auth-shell">
      <div class="card auth-card stack">
        <app-logo link="${home}" />
        <h1 class="page-title">{{ 'nav.login' | t }}</h1>
        <form (ngSubmit)="login()">
          <div class="field"><label>{{ 'auth.email' | t }}</label><input type="email" [(ngModel)]="email" name="email" required /></div>
          <div class="field"><label>{{ 'auth.password' | t }}</label><input type="password" [(ngModel)]="password" name="password" required /></div>
          <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'nav.login' | t }}</button>
        </form>
        <a routerLink="/${portal}/forgot-password">{{ 'auth.forgot' | t }}</a>
      </div>
    </div>
  \`,
})
export class ${className} {
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  email = ''; password = '';
  readonly busy = signal(false);
  login(): void {
    this.busy.set(true);
    this.auth.passwordLogin('${portal}', { email: this.email, password: this.password }).subscribe({
      next: () => { this.toast.success('Welcome'); void this.router.navigateByUrl('${home}'); },
      error: () => this.busy.set(false),
    });
  }
}
`;

write('provider/auth/provider-login.component.ts', passwordAuth('provider', 'app-provider-login', 'ProviderLoginComponent', '/provider'));
write('admin/auth/admin-login.component.ts', passwordAuth('admin', 'app-admin-login', 'AdminLoginComponent', '/admin'));

const forgot = (portal, selector, className) => `import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { LogoComponent } from '../../../shared/components/logo/logo.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: '${selector}',
  standalone: true,
  imports: [FormsModule, RouterLink, LogoComponent, TranslatePipe],
  template: \`
    <div class="auth-shell"><div class="card auth-card stack">
      <app-logo /><h1 class="page-title">{{ 'auth.forgot' | t }}</h1>
      @if (!sent()) {
        <form (ngSubmit)="send()"><div class="field"><label>{{ 'auth.email' | t }}</label><input [(ngModel)]="email" name="email" /></div>
        <button class="btn btn-primary" type="submit">{{ 'auth.sendOtp' | t }}</button></form>
      } @else {
        <form (ngSubmit)="reset()">
          @if (devOtp()) { <p class="badge">Dev OTP: {{ devOtp() }}</p> }
          <div class="field"><label>{{ 'auth.otp' | t }}</label><input [(ngModel)]="otp" name="otp" /></div>
          <div class="field"><label>{{ 'auth.newPassword' | t }}</label><input type="password" [(ngModel)]="newPassword" name="newPassword" /></div>
          <button class="btn btn-primary" type="submit">{{ 'auth.reset' | t }}</button>
        </form>
      }
      <a routerLink="/${portal}/login">{{ 'nav.login' | t }}</a>
    </div></div>
  \`,
})
export class ${className} {
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  email = ''; otp = ''; newPassword = '';
  readonly sent = signal(false);
  readonly devOtp = signal<string | null>(null);
  send(): void {
    this.auth.forgotPassword('${portal}', { email: this.email }).subscribe({
      next: (r) => { this.sent.set(true); this.devOtp.set(r.developmentOtp ?? null); this.toast.success(r.message || 'OTP sent'); },
    });
  }
  reset(): void {
    this.auth.resetPassword('${portal}', { email: this.email, otp: this.otp, newPassword: this.newPassword }).subscribe({
      next: () => this.toast.success('Password updated'),
    });
  }
}
`;

write('provider/auth/provider-forgot.component.ts', forgot('provider', 'app-provider-forgot', 'ProviderForgotComponent'));
write('admin/auth/admin-forgot.component.ts', forgot('admin', 'app-admin-forgot', 'AdminForgotComponent'));

write('provider/dashboard/provider-shell.component.ts', `import { Component } from '@angular/core';
import { DashboardLayoutComponent, NavItem } from '../../../layouts/dashboard-layout/dashboard-layout.component';

@Component({
  selector: 'app-provider-shell',
  standalone: true,
  imports: [DashboardLayoutComponent],
  template: \`<app-dashboard-layout portal="provider" title="Provider" homeLink="/provider" [items]="items" />\`,
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
`);

write('admin/dashboard/admin-shell.component.ts', `import { Component } from '@angular/core';
import { DashboardLayoutComponent, NavItem } from '../../../layouts/dashboard-layout/dashboard-layout.component';

@Component({
  selector: 'app-admin-shell',
  standalone: true,
  imports: [DashboardLayoutComponent],
  template: \`<app-dashboard-layout portal="admin" title="Admin" homeLink="/admin" [items]="items" />\`,
})
export class AdminShellComponent {
  items: NavItem[] = [
    { labelKey: 'nav.dashboard', link: '/admin' },
    { labelKey: 'nav.providers', link: '/admin/providers', permissions: ['Providers.Read'] },
    { labelKey: 'nav.services', link: '/admin/services', permissions: ['Services.Read'] },
    { labelKey: 'nav.clients', link: '/admin/clients', permissions: ['Clients.Read'] },
    { labelKey: 'nav.orders', link: '/admin/orders', permissions: ['Orders.Read'] },
    { labelKey: 'nav.roles', link: '/admin/roles', permissions: ['Roles.Read'] },
    { labelKey: 'nav.users', link: '/admin/users', permissions: ['Admins.Read'] },
    { labelKey: 'nav.notifications', link: '/admin/notifications' },
    { labelKey: 'nav.profile', link: '/admin/profile' },
  ];
}
`);

write('provider/dashboard/provider-home.component.ts', `import { Component } from '@angular/core';
@Component({
  selector: 'app-provider-home',
  standalone: true,
  template: \`<div class="card"><h1 class="page-title">Merchant dashboard</h1><p class="muted">Manage catalog, orders, and staff.</p></div>\`,
})
export class ProviderHomeComponent {}
`);

write('admin/dashboard/admin-home.component.ts', `import { Component } from '@angular/core';
@Component({
  selector: 'app-admin-home',
  standalone: true,
  template: \`<div class="card"><h1 class="page-title">Admin console</h1><p class="muted">Oversee providers, services, clients, and orders.</p></div>\`,
})
export class AdminHomeComponent {}
`);

// Provider store
write('provider/store/provider-store.component.ts', `import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ProviderStoreDto } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-provider-store',
  standalone: true,
  imports: [FormsModule, TranslatePipe],
  template: \`
    <h1 class="page-title">{{ 'nav.store' | t }}</h1>
    @if (store()) {
      <form class="card stack" (ngSubmit)="save()">
        @if (store()!.imageUrl) { <img class="thumb-lg" [src]="store()!.imageUrl!" alt="store" /> }
        <div class="field"><label>Company</label><input [(ngModel)]="companyName" name="companyName" /></div>
        <div class="field"><label>Phone</label><input [(ngModel)]="phoneNumber" name="phoneNumber" /></div>
        <p class="muted">Service: {{ store()!.serviceName }}</p>
        <input type="file" accept="image/*" (change)="onFile($event)" />
        <button class="btn btn-primary" type="submit">{{ 'actions.save' | t }}</button>
      </form>
    }
  \`,
})
export class ProviderStoreComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  readonly store = signal<ProviderStoreDto | null>(null);
  companyName = ''; phoneNumber = '';
  ngOnInit(): void {
    this.api.get<ProviderStoreDto>('/provider/store').subscribe({
      next: (s) => { this.store.set(s); this.companyName = s.companyName; this.phoneNumber = s.phoneNumber || ''; },
    });
  }
  save(): void {
    this.api.put<ProviderStoreDto>('/provider/store', { companyName: this.companyName, phoneNumber: this.phoneNumber }).subscribe({
      next: (s) => { this.store.set(s); this.toast.success('Saved'); },
    });
  }
  onFile(ev: Event): void {
    const file = (ev.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.api.upload('/provider/profile/image', file).subscribe({ next: () => { this.toast.success('Image uploaded'); this.ngOnInit(); } });
  }
}
`);

// Provider categories
write('provider/categories/provider-categories.component.ts', `import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CategoryDetailDto, PagedResult } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-provider-categories',
  standalone: true,
  imports: [FormsModule, EmptyStateComponent, PaginationComponent, TranslatePipe],
  template: \`
    <div class="toolbar">
      <h1 class="page-title">{{ 'nav.categories' | t }}</h1>
      @if (canCreate) { <button class="btn btn-primary" type="button" (click)="showForm=true">{{ 'actions.create' | t }}</button> }
    </div>
    @if (showForm) {
      <form class="card stack" (ngSubmit)="create()">
        <div class="field"><label>Name (EN)</label><input [(ngModel)]="form.nameEn" name="nameEn" required /></div>
        <div class="field"><label>Description (EN)</label><textarea [(ngModel)]="form.descEn" name="descEn"></textarea></div>
        <div class="field"><label>Display order</label><input type="number" [(ngModel)]="form.displayOrder" name="displayOrder" /></div>
        <div class="row">
          <button class="btn btn-primary" type="submit">{{ 'actions.save' | t }}</button>
          <button class="btn btn-ghost" type="button" (click)="showForm=false">{{ 'actions.cancel' | t }}</button>
        </div>
      </form>
    }
    @if (!items().length) { <app-empty-state /> } @else {
      <div class="table-wrap card"><table class="data">
        <thead><tr><th>Name</th><th>Order</th><th>Active</th><th></th></tr></thead>
        <tbody>
          @for (c of items(); track c.id) {
            <tr>
              <td>{{ c.name }}</td><td>{{ c.displayOrder }}</td>
              <td><span class="badge" [class.badge-success]="c.isActive">{{ c.isActive ? 'Yes' : 'No' }}</span></td>
              <td class="row">
                @if (canUpdate) {
                  <button class="btn btn-ghost" type="button" (click)="toggle(c)">{{ c.isActive ? ('actions.deactivate'|t) : ('actions.activate'|t) }}</button>
                  <input type="file" (change)="upload(c.id, $event)" />
                }
                @if (canDelete) { <button class="btn btn-danger" type="button" (click)="remove(c.id)">{{ 'actions.delete' | t }}</button> }
              </td>
            </tr>
          }
        </tbody>
      </table></div>
      <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" (change)="load($event)" />
    }
  \`,
})
export class ProviderCategoriesComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  readonly items = signal<CategoryDetailDto[]>([]);
  showForm = false; page = 1; totalPages = 1; totalCount = 0;
  form = { nameEn: '', descEn: '', displayOrder: 0 };
  canCreate = this.tokens.hasPermission('provider', 'ProviderCategory.Create');
  canUpdate = this.tokens.hasPermission('provider', 'ProviderCategory.Update');
  canDelete = this.tokens.hasPermission('provider', 'ProviderCategory.Delete');

  ngOnInit(): void { this.load(1); }
  load(page: number): void {
    this.page = page;
    this.api.get<PagedResult<CategoryDetailDto>>('/provider/categories', { pageNumber: page, pageSize: 20 }).subscribe({
      next: (r) => { this.items.set(r.items ?? []); this.totalPages = r.totalPages; this.totalCount = r.totalCount; },
    });
  }
  create(): void {
    this.api.post('/provider/categories', {
      displayOrder: this.form.displayOrder,
      isActive: true,
      name: { en: this.form.nameEn },
      description: { en: this.form.descEn || null },
    }).subscribe({ next: () => { this.toast.success('Created'); this.showForm = false; this.load(1); } });
  }
  toggle(c: CategoryDetailDto): void {
    this.api.post(\`/provider/categories/\${c.id}/set-active\`, { isActive: !c.isActive }).subscribe({ next: () => this.load(this.page) });
  }
  remove(id: string): void {
    this.api.delete(\`/provider/categories/\${id}\`).subscribe({ next: () => this.load(this.page) });
  }
  upload(id: string, ev: Event): void {
    const file = (ev.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.api.upload(\`/provider/categories/\${id}/image\`, file).subscribe({ next: () => this.toast.success('Uploaded') });
  }
}
`);

console.log('provider batch partial done');
