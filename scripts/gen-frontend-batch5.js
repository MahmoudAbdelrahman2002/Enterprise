const fs = require('fs');
const path = require('path');
const root = path.join('frontend', 'subito-web', 'src', 'app', 'features');
const write = (rel, content) => {
  const p = path.join(root, rel);
  fs.mkdirSync(path.dirname(p), { recursive: true });
  fs.writeFileSync(p, content);
  console.log(rel);
};

write('admin/providers/admin-providers.component.ts', `import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MarketplaceServiceLookupDto, PagedResult, ProviderAdminDto } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-providers',
  standalone: true,
  imports: [FormsModule, RouterLink, EmptyStateComponent, PaginationComponent, TranslatePipe],
  template: \`
    <div class="toolbar">
      <h1 class="page-title">{{ 'nav.providers' | t }}</h1>
      <div class="row">
        <input [(ngModel)]="search" (keyup.enter)="load(1)" [placeholder]="'actions.search' | t" />
        @if (canCreate) { <button class="btn btn-primary" type="button" (click)="showForm=true">{{ 'actions.create' | t }}</button> }
      </div>
    </div>
    @if (showForm) {
      <form class="card stack" (ngSubmit)="create()">
        <div class="field"><label>Email</label><input [(ngModel)]="form.email" name="email" /></div>
        <div class="field"><label>Password</label><input type="password" [(ngModel)]="form.password" name="password" /></div>
        <div class="field"><label>First name</label><input [(ngModel)]="form.firstName" name="firstName" /></div>
        <div class="field"><label>Last name</label><input [(ngModel)]="form.lastName" name="lastName" /></div>
        <div class="field"><label>Company</label><input [(ngModel)]="form.companyName" name="companyName" /></div>
        <div class="field"><label>Phone</label><input [(ngModel)]="form.phoneNumber" name="phoneNumber" /></div>
        <div class="field"><label>Service</label>
          <select [(ngModel)]="form.serviceId" name="serviceId">
            <option [ngValue]="null">—</option>
            @for (s of services(); track s.id) { <option [value]="s.id">{{ s.name }}</option> }
          </select>
        </div>
        <div class="row">
          <button class="btn btn-primary" type="submit">{{ 'actions.save' | t }}</button>
          <button class="btn btn-ghost" type="button" (click)="showForm=false">{{ 'actions.cancel' | t }}</button>
        </div>
      </form>
    }
    @if (!items().length) { <app-empty-state /> } @else {
      <div class="table-wrap card"><table class="data">
        <thead><tr><th>Company</th><th>Email</th><th>Service</th><th>Active</th><th></th></tr></thead>
        <tbody>
          @for (p of items(); track p.id) {
            <tr>
              <td>{{ p.companyName }}</td><td>{{ p.email }}</td><td>{{ p.serviceName }}</td>
              <td>{{ p.isActive }}</td>
              <td class="row">
                <a [routerLink]="['/admin/providers', p.id]">View</a>
                @if (canUpdate) {
                  <button class="btn btn-ghost" type="button" (click)="toggle(p)">{{ p.isActive ? ('actions.deactivate'|t) : ('actions.activate'|t) }}</button>
                }
                @if (canDelete) { <button class="btn btn-danger" type="button" (click)="remove(p.id)">{{ 'actions.delete' | t }}</button> }
              </td>
            </tr>
          }
        </tbody>
      </table></div>
      <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" (change)="load($event)" />
    }
  \`,
})
export class AdminProvidersComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  readonly items = signal<ProviderAdminDto[]>([]);
  readonly services = signal<MarketplaceServiceLookupDto[]>([]);
  search = ''; showForm = false; page = 1; totalPages = 1; totalCount = 0;
  form: any = { email: '', password: '', firstName: '', lastName: '', companyName: '', phoneNumber: '', serviceId: null };
  canCreate = this.tokens.hasPermission('admin', 'Providers.Create');
  canUpdate = this.tokens.hasPermission('admin', 'Providers.Update');
  canDelete = this.tokens.hasPermission('admin', 'Providers.Delete');

  ngOnInit(): void {
    this.api.get<MarketplaceServiceLookupDto[]>('/admin/services/lookup').subscribe({ next: (s) => this.services.set(s ?? []) });
    this.load(1);
  }
  load(page: number): void {
    this.page = page;
    this.api.get<PagedResult<ProviderAdminDto>>('/admin/providers', { pageNumber: page, pageSize: 20, searchTerm: this.search || null }).subscribe({
      next: (r) => { this.items.set(r.items ?? []); this.totalPages = r.totalPages; this.totalCount = r.totalCount; },
    });
  }
  create(): void {
    this.api.post('/admin/providers', this.form).subscribe({ next: () => { this.toast.success('Created'); this.showForm = false; this.load(1); } });
  }
  toggle(p: ProviderAdminDto): void {
    this.api.post(\`/admin/providers/\${p.id}/set-active\`, { isActive: !p.isActive }).subscribe({ next: () => this.load(this.page) });
  }
  remove(id: string): void {
    this.api.delete(\`/admin/providers/\${id}\`).subscribe({ next: () => this.load(this.page) });
  }
}
`);

write('admin/providers/admin-provider-detail.component.ts', `import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CategoryDetailDto, MarketplaceServiceLookupDto, ProductListItemDto, ProviderAdminDto } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-provider-detail',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslatePipe],
  template: \`
    <div class="toolbar"><h1 class="page-title">Provider</h1><a routerLink="/admin/providers" class="btn btn-ghost">{{ 'actions.back' | t }}</a></div>
    @if (provider()) {
      <form class="card stack" (ngSubmit)="save()">
        @if (provider()!.imageUrl) { <img class="thumb-lg" [src]="provider()!.imageUrl!" alt="" /> }
        <div class="field"><label>Company</label><input [(ngModel)]="form.companyName" name="companyName" [disabled]="!canUpdate" /></div>
        <div class="field"><label>First name</label><input [(ngModel)]="form.firstName" name="firstName" [disabled]="!canUpdate" /></div>
        <div class="field"><label>Last name</label><input [(ngModel)]="form.lastName" name="lastName" [disabled]="!canUpdate" /></div>
        <div class="field"><label>Phone</label><input [(ngModel)]="form.phoneNumber" name="phoneNumber" [disabled]="!canUpdate" /></div>
        <div class="field"><label>Service</label>
          <select [(ngModel)]="form.serviceId" name="serviceId" [disabled]="!canUpdate">
            @for (s of services(); track s.id) { <option [value]="s.id">{{ s.name }}</option> }
          </select>
        </div>
        <p class="muted">{{ provider()!.email }}</p>
        @if (canUpdate) {
          <input type="file" (change)="upload($event)" />
          <button class="btn btn-primary" type="submit">{{ 'actions.save' | t }}</button>
        }
      </form>
      <div class="card" style="margin-top:1rem">
        <h3>Categories</h3>
        <ul>@for (c of categories(); track c.id) { <li>{{ c.name }} ({{ c.isActive ? 'active' : 'inactive' }})</li> }</ul>
        <h3>Products</h3>
        <ul>@for (p of products(); track p.id) { <li>{{ p.name }} — {{ p.sku }}</li> }</ul>
      </div>
    }
  \`,
})
export class AdminProviderDetailComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  readonly provider = signal<ProviderAdminDto | null>(null);
  readonly services = signal<MarketplaceServiceLookupDto[]>([]);
  readonly categories = signal<CategoryDetailDto[]>([]);
  readonly products = signal<ProductListItemDto[]>([]);
  form: any = {};
  canUpdate = this.tokens.hasPermission('admin', 'Providers.Update');
  id = '';

  ngOnInit(): void {
    this.id = this.route.snapshot.paramMap.get('id')!;
    this.api.get<MarketplaceServiceLookupDto[]>('/admin/services/lookup').subscribe({ next: (s) => this.services.set(s ?? []) });
    this.api.get<ProviderAdminDto>(\`/admin/providers/\${this.id}\`).subscribe({
      next: (p) => {
        this.provider.set(p);
        this.form = { firstName: p.firstName, lastName: p.lastName, companyName: p.companyName, phoneNumber: p.phoneNumber, serviceId: p.serviceId };
      },
    });
    this.api.get<CategoryDetailDto[]>(\`/admin/providers/\${this.id}/categories\`).subscribe({ next: (c) => this.categories.set(c ?? []) });
    this.api.get<ProductListItemDto[]>(\`/admin/providers/\${this.id}/products\`).subscribe({ next: (p) => this.products.set(p ?? []) });
  }
  save(): void {
    this.api.put(\`/admin/providers/\${this.id}\`, this.form).subscribe({ next: (p: any) => { this.provider.set(p); this.toast.success('Saved'); } });
  }
  upload(ev: Event): void {
    const file = (ev.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.api.upload(\`/admin/providers/\${this.id}/image\`, file).subscribe({ next: () => { this.toast.success('Uploaded'); this.ngOnInit(); } });
  }
}
`);

write('admin/services/admin-services.component.ts', `import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MarketplaceServiceDto, PagedResult } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-services',
  standalone: true,
  imports: [FormsModule, EmptyStateComponent, PaginationComponent, TranslatePipe],
  template: \`
    <div class="toolbar">
      <h1 class="page-title">{{ 'nav.services' | t }}</h1>
      @if (canCreate) { <button class="btn btn-primary" type="button" (click)="showForm=true">{{ 'actions.create' | t }}</button> }
    </div>
    @if (showForm) {
      <form class="card stack" (ngSubmit)="create()">
        <div class="field"><label>Code</label><input [(ngModel)]="form.code" name="code" /></div>
        <div class="field"><label>Name EN</label><input [(ngModel)]="form.nameEn" name="nameEn" /></div>
        <div class="field"><label>Name AR</label><input [(ngModel)]="form.nameAr" name="nameAr" /></div>
        <div class="field"><label>Name IT</label><input [(ngModel)]="form.nameIt" name="nameIt" /></div>
        <div class="field"><label>Display order</label><input type="number" [(ngModel)]="form.displayOrder" name="displayOrder" /></div>
        <div class="row">
          <button class="btn btn-primary" type="submit">{{ 'actions.save' | t }}</button>
          <button class="btn btn-ghost" type="button" (click)="showForm=false">{{ 'actions.cancel' | t }}</button>
        </div>
      </form>
    }
    @if (!items().length) { <app-empty-state /> } @else {
      <div class="table-wrap card"><table class="data">
        <thead><tr><th>Code</th><th>Name</th><th>Order</th><th>Active</th><th></th></tr></thead>
        <tbody>
          @for (s of items(); track s.id) {
            <tr>
              <td>{{ s.code }}</td><td>{{ s.name }}</td><td>{{ s.displayOrder }}</td><td>{{ s.isActive }}</td>
              <td class="row">
                @if (canUpdate) {
                  <button class="btn btn-ghost" type="button" (click)="toggle(s)">{{ s.isActive ? ('actions.deactivate'|t) : ('actions.activate'|t) }}</button>
                  <input type="file" (change)="upload(s.id, $event)" />
                }
                @if (canDelete) { <button class="btn btn-danger" type="button" (click)="remove(s.id)">{{ 'actions.delete' | t }}</button> }
              </td>
            </tr>
          }
        </tbody>
      </table></div>
      <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" (change)="load($event)" />
    }
  \`,
})
export class AdminServicesComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  readonly items = signal<MarketplaceServiceDto[]>([]);
  showForm = false; page = 1; totalPages = 1; totalCount = 0;
  form = { code: '', nameEn: '', nameAr: '', nameIt: '', displayOrder: 0 };
  canCreate = this.tokens.hasPermission('admin', 'Services.Create');
  canUpdate = this.tokens.hasPermission('admin', 'Services.Update');
  canDelete = this.tokens.hasPermission('admin', 'Services.Delete');

  ngOnInit(): void { this.load(1); }
  load(page: number): void {
    this.page = page;
    this.api.get<PagedResult<MarketplaceServiceDto>>('/admin/services', { pageNumber: page, pageSize: 20 }).subscribe({
      next: (r) => { this.items.set(r.items ?? []); this.totalPages = r.totalPages; this.totalCount = r.totalCount; },
    });
  }
  create(): void {
    this.api.post('/admin/services', {
      code: this.form.code,
      displayOrder: this.form.displayOrder,
      isActive: true,
      name: { en: this.form.nameEn, ar: this.form.nameAr || null, it: this.form.nameIt || null },
    }).subscribe({ next: () => { this.toast.success('Created'); this.showForm = false; this.load(1); } });
  }
  toggle(s: MarketplaceServiceDto): void {
    this.api.post(\`/admin/services/\${s.id}/set-active\`, { isActive: !s.isActive }).subscribe({ next: () => this.load(this.page) });
  }
  remove(id: string): void {
    this.api.delete(\`/admin/services/\${id}\`).subscribe({ next: () => this.load(this.page) });
  }
  upload(id: string, ev: Event): void {
    const file = (ev.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.api.upload(\`/admin/services/\${id}/image\`, file).subscribe({ next: () => this.toast.success('Uploaded') });
  }
}
`);

write('admin/clients/admin-clients.component.ts', `import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminClientDto, PagedResult } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-clients',
  standalone: true,
  imports: [FormsModule, EmptyStateComponent, PaginationComponent, TranslatePipe],
  template: \`
    <div class="toolbar">
      <h1 class="page-title">{{ 'nav.clients' | t }}</h1>
      <input [(ngModel)]="search" (keyup.enter)="load(1)" [placeholder]="'actions.search' | t" />
    </div>
    @if (!items().length) { <app-empty-state /> } @else {
      <div class="table-wrap card"><table class="data">
        <thead><tr><th>Name</th><th>Email</th><th>Active</th><th></th></tr></thead>
        <tbody>
          @for (c of items(); track c.id) {
            <tr>
              <td>{{ c.firstName }} {{ c.lastName }}</td><td>{{ c.email }}</td><td>{{ c.isActive }}</td>
              <td>
                @if (canUpdate) {
                  <button class="btn btn-ghost" type="button" (click)="toggle(c)">{{ c.isActive ? ('actions.deactivate'|t) : ('actions.activate'|t) }}</button>
                }
              </td>
            </tr>
          }
        </tbody>
      </table></div>
      <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" (change)="load($event)" />
    }
  \`,
})
export class AdminClientsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly tokens = inject(TokenStoreService);
  readonly items = signal<AdminClientDto[]>([]);
  search = ''; page = 1; totalPages = 1; totalCount = 0;
  canUpdate = this.tokens.hasPermission('admin', 'Clients.Update');
  ngOnInit(): void { this.load(1); }
  load(page: number): void {
    this.page = page;
    this.api.get<PagedResult<AdminClientDto>>('/admin/clients', { pageNumber: page, pageSize: 20, searchTerm: this.search || null }).subscribe({
      next: (r) => { this.items.set(r.items ?? []); this.totalPages = r.totalPages; this.totalCount = r.totalCount; },
    });
  }
  toggle(c: AdminClientDto): void {
    this.api.post(\`/admin/clients/\${c.id}/set-active\`, { isActive: !c.isActive }).subscribe({ next: () => this.load(this.page) });
  }
}
`);

write('admin/orders/admin-orders.component.ts', `import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ORDER_STATUS_LABELS } from '../../../core/models/api.models';
import { OrderDetailDto, OrderListItemDto, PagedResult } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe, EmptyStateComponent, PaginationComponent, TranslatePipe],
  template: \`
    <h1 class="page-title">{{ 'nav.orders' | t }}</h1>
    @if (!selected()) {
      @if (!items().length) { <app-empty-state /> } @else {
        <div class="table-wrap card"><table class="data">
          <thead><tr><th>Date</th><th>Provider</th><th>Total</th><th>Status</th><th></th></tr></thead>
          <tbody>
            @for (o of items(); track o.id) {
              <tr>
                <td>{{ o.orderDateUtc | date:'medium' }}</td>
                <td>{{ o.providerId }}</td>
                <td>{{ o.totalAmount | number:'1.2-2' }}</td>
                <td>{{ labels[o.status] }}</td>
                <td><a [routerLink]="['/admin/orders', o.id]">View</a></td>
              </tr>
            }
          </tbody>
        </table></div>
        <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" (change)="load($event)" />
      }
    } @else {
      <div class="card stack">
        <a routerLink="/admin/orders">{{ 'actions.back' | t }}</a>
        <span class="badge">{{ labels[selected()!.status] }}</span>
        @for (i of selected()!.items; track i.id) {
          <div class="row" style="justify-content:space-between"><span>{{ i.productName }} × {{ i.quantity }}</span><span>{{ i.lineTotal | number:'1.2-2' }}</span></div>
        }
        <strong>{{ selected()!.totalAmount | number:'1.2-2' }}</strong>
      </div>
    }
  \`,
})
export class AdminOrdersComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  readonly items = signal<OrderListItemDto[]>([]);
  readonly selected = signal<OrderDetailDto | null>(null);
  readonly labels = ORDER_STATUS_LABELS;
  page = 1; totalPages = 1; totalCount = 0;
  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('orderId');
    if (id) this.api.get<OrderDetailDto>(\`/admin/orders/\${id}\`).subscribe({ next: (o) => this.selected.set(o) });
    else this.load(1);
  }
  load(page: number): void {
    this.page = page;
    this.api.get<PagedResult<OrderListItemDto>>('/admin/orders', { pageNumber: page, pageSize: 20 }).subscribe({
      next: (r) => { this.items.set(r.items ?? []); this.totalPages = r.totalPages; this.totalCount = r.totalCount; },
    });
  }
}
`);

write('admin/roles/admin-roles.component.ts', `import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PermissionGroupDto, RoleDetailDto, RoleListItemDto, PagedResult } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-roles',
  standalone: true,
  imports: [FormsModule, EmptyStateComponent, TranslatePipe],
  template: \`
    <div class="toolbar">
      <h1 class="page-title">{{ 'nav.roles' | t }}</h1>
      @if (canCreate) { <button class="btn btn-primary" type="button" (click)="startCreate()">{{ 'actions.create' | t }}</button> }
    </div>
    @if (editing()) {
      <form class="card stack" (ngSubmit)="save()">
        <div class="field"><label>Name</label><input [(ngModel)]="name" name="name" [disabled]="editing()!.isSystem" /></div>
        @for (g of groups(); track g.module) {
          <fieldset><legend>{{ g.module }}</legend>
            @for (p of g.permissions; track p.name) {
              <label style="display:block;margin:.25rem 0">
                <input type="checkbox" [checked]="selected.has(p.name)" (change)="toggle(p.name,$event)" [disabled]="editing()!.isSystem" />
                {{ p.name }}
              </label>
            }
          </fieldset>
        }
        <div class="row">
          @if (!editing()!.isSystem) { <button class="btn btn-primary" type="submit">{{ 'actions.save' | t }}</button> }
          <button class="btn btn-ghost" type="button" (click)="editing.set(null)">{{ 'actions.cancel' | t }}</button>
        </div>
      </form>
    } @else if (!items().length) { <app-empty-state /> } @else {
      <div class="table-wrap card"><table class="data">
        <thead><tr><th>Name</th><th>Users</th><th>System</th><th></th></tr></thead>
        <tbody>
          @for (r of items(); track r.id) {
            <tr>
              <td>{{ r.name }}</td><td>{{ r.usersCount }}</td><td>{{ r.isSystem }}</td>
              <td class="row">
                <button class="btn btn-ghost" type="button" (click)="edit(r)">{{ 'actions.edit' | t }}</button>
                @if (canDelete && !r.isSystem) { <button class="btn btn-danger" type="button" (click)="remove(r.id)">{{ 'actions.delete' | t }}</button> }
              </td>
            </tr>
          }
        </tbody>
      </table></div>
    }
  \`,
})
export class AdminRolesComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  readonly items = signal<RoleListItemDto[]>([]);
  readonly groups = signal<PermissionGroupDto[]>([]);
  readonly editing = signal<RoleDetailDto | null>(null);
  name = ''; selected = new Set<string>();
  canCreate = this.tokens.hasPermission('admin', 'Roles.Create');
  canDelete = this.tokens.hasPermission('admin', 'Roles.Delete');

  ngOnInit(): void {
    this.api.get<PermissionGroupDto[]>('/admin/permissions').subscribe({ next: (g) => this.groups.set(g ?? []) });
    this.reload();
  }
  reload(): void {
    this.api.get<PagedResult<RoleListItemDto>>('/admin/roles', { pageSize: 50 }).subscribe({ next: (r) => this.items.set(r.items ?? []) });
  }
  startCreate(): void {
    this.editing.set({ id: '', name: '', roleType: 'Admin', providerId: null, isSystem: false, usersCount: 0, permissions: [] });
    this.name = ''; this.selected = new Set();
  }
  edit(r: RoleListItemDto): void {
    this.api.get<RoleDetailDto>(\`/admin/roles/\${r.id}\`).subscribe({
      next: (d) => { this.editing.set(d); this.name = d.name; this.selected = new Set(d.permissions); },
    });
  }
  toggle(name: string, ev: Event): void {
    const checked = (ev.target as HTMLInputElement).checked;
    if (checked) this.selected.add(name); else this.selected.delete(name);
  }
  save(): void {
    const current = this.editing();
    if (!current || current.isSystem) return;
    const body = { name: this.name, permissions: [...this.selected] };
    const req = current.id ? this.api.put(\`/admin/roles/\${current.id}\`, body) : this.api.post('/admin/roles', body);
    req.subscribe({ next: () => { this.toast.success('Saved'); this.editing.set(null); this.reload(); } });
  }
  remove(id: string): void {
    this.api.delete(\`/admin/roles/\${id}\`).subscribe({ next: () => this.reload() });
  }
}
`);

write('admin/users/admin-users.component.ts', `import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PagedResult, RoleListItemDto, StaffDetailDto, StaffListItemDto } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [FormsModule, EmptyStateComponent, TranslatePipe],
  template: \`
    <div class="toolbar">
      <h1 class="page-title">{{ 'nav.users' | t }}</h1>
      @if (canCreate) { <button class="btn btn-primary" type="button" (click)="showForm=true">{{ 'actions.create' | t }}</button> }
    </div>
    @if (showForm) {
      <form class="card stack" (ngSubmit)="create()">
        <div class="field"><label>First name</label><input [(ngModel)]="form.firstName" name="firstName" /></div>
        <div class="field"><label>Last name</label><input [(ngModel)]="form.lastName" name="lastName" /></div>
        <div class="field"><label>Email</label><input [(ngModel)]="form.email" name="email" /></div>
        <div class="field"><label>Password</label><input type="password" [(ngModel)]="form.password" name="password" /></div>
        <div class="field"><label>Role</label>
          <select [(ngModel)]="form.roleId" name="roleId">
            @for (r of roles(); track r.id) { <option [value]="r.id">{{ r.name }}</option> }
          </select>
        </div>
        <div class="row">
          <button class="btn btn-primary" type="submit">{{ 'actions.save' | t }}</button>
          <button class="btn btn-ghost" type="button" (click)="showForm=false">{{ 'actions.cancel' | t }}</button>
        </div>
      </form>
    }
    @if (!items().length) { <app-empty-state /> } @else {
      <div class="table-wrap card"><table class="data">
        <thead><tr><th>Name</th><th>Email</th><th>Role</th><th>Active</th><th></th></tr></thead>
        <tbody>
          @for (u of items(); track u.id) {
            <tr>
              <td>{{ u.firstName }} {{ u.lastName }}</td><td>{{ u.email }}</td><td>{{ u.roleName }}</td><td>{{ u.isActive }}</td>
              <td class="row">
                @if (canUpdate && !u.isSystem) {
                  <button class="btn btn-ghost" type="button" (click)="toggle(u)">{{ u.isActive ? ('actions.deactivate'|t) : ('actions.activate'|t) }}</button>
                }
                @if (canDelete && !u.isSystem) {
                  <button class="btn btn-danger" type="button" (click)="remove(u.id)">{{ 'actions.delete' | t }}</button>
                }
              </td>
            </tr>
          }
        </tbody>
      </table></div>
    }
  \`,
})
export class AdminUsersComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  readonly items = signal<StaffListItemDto[]>([]);
  readonly roles = signal<RoleListItemDto[]>([]);
  showForm = false;
  form = { firstName: '', lastName: '', email: '', password: '', roleId: '', phoneNumber: '' };
  canCreate = this.tokens.hasPermission('admin', 'Admins.Create');
  canUpdate = this.tokens.hasPermission('admin', 'Admins.Update');
  canDelete = this.tokens.hasPermission('admin', 'Admins.Delete');

  ngOnInit(): void {
    this.api.get<PagedResult<RoleListItemDto>>('/admin/roles', { pageSize: 100 }).subscribe({ next: (r) => this.roles.set(r.items ?? []) });
    this.reload();
  }
  reload(): void {
    this.api.get<PagedResult<StaffListItemDto>>('/admin/users', { pageSize: 50 }).subscribe({ next: (r) => this.items.set(r.items ?? []) });
  }
  create(): void {
    this.api.post<StaffDetailDto>('/admin/users', this.form).subscribe({
      next: () => { this.toast.success('Created'); this.showForm = false; this.reload(); },
    });
  }
  toggle(u: StaffListItemDto): void {
    this.api.post(\`/admin/users/\${u.id}/set-active\`, { isActive: !u.isActive }).subscribe({ next: () => this.reload() });
  }
  remove(id: string): void {
    this.api.delete(\`/admin/users/\${id}\`).subscribe({ next: () => this.reload() });
  }
}
`);

write('admin/profile/admin-profile.component.ts', `import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ProfileDto } from '../../../core/models/api.models';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-profile',
  standalone: true,
  imports: [FormsModule, TranslatePipe],
  template: \`
    <h1 class="page-title">{{ 'nav.profile' | t }}</h1>
    @if (profile()) {
      <form class="card stack" (ngSubmit)="save()">
        <div class="field"><label>{{ 'auth.firstName' | t }}</label><input [(ngModel)]="firstName" name="firstName" /></div>
        <div class="field"><label>{{ 'auth.lastName' | t }}</label><input [(ngModel)]="lastName" name="lastName" /></div>
        <div class="field"><label>{{ 'auth.email' | t }}</label><input [value]="profile()!.email" disabled /></div>
        <button class="btn btn-primary" type="submit">{{ 'actions.save' | t }}</button>
      </form>
      <form class="card stack" style="margin-top:1rem" (ngSubmit)="changePassword()">
        <h3>{{ 'auth.password' | t }}</h3>
        <div class="field"><label>{{ 'auth.currentPassword' | t }}</label><input type="password" [(ngModel)]="currentPassword" name="currentPassword" /></div>
        <div class="field"><label>{{ 'auth.newPassword' | t }}</label><input type="password" [(ngModel)]="newPassword" name="newPassword" /></div>
        <button class="btn btn-secondary" type="submit">{{ 'actions.save' | t }}</button>
      </form>
    }
  \`,
})
export class AdminProfileComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  readonly profile = signal<ProfileDto | null>(null);
  firstName = ''; lastName = ''; currentPassword = ''; newPassword = '';
  ngOnInit(): void {
    this.auth.getProfile('admin').subscribe({ next: (p) => { this.profile.set(p); this.firstName = p.firstName; this.lastName = p.lastName; } });
  }
  save(): void {
    this.auth.updateProfile('admin', { firstName: this.firstName, lastName: this.lastName }).subscribe({
      next: (p) => { this.profile.set(p); this.toast.success('Saved'); },
    });
  }
  changePassword(): void {
    this.auth.changePassword('admin', { currentPassword: this.currentPassword, newPassword: this.newPassword }).subscribe({
      next: () => this.toast.success('Password changed'),
    });
  }
}
`);

write('admin/notifications/admin-notifications.component.ts', `import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { NotificationDto, PagedResult } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-notifications',
  standalone: true,
  imports: [DatePipe, EmptyStateComponent, TranslatePipe],
  template: \`
    <h1 class="page-title">{{ 'nav.notifications' | t }}</h1>
    @if (!items().length) { <app-empty-state /> } @else {
      <div class="stack">
        @for (n of items(); track n.id) {
          <article class="card" (click)="mark(n)">
            <strong>{{ n.title }}</strong><p>{{ n.body }}</p>
            <span class="muted">{{ n.createdAtUtc | date:'medium' }}</span>
          </article>
        }
      </div>
    }
  \`,
})
export class AdminNotificationsComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly items = signal<NotificationDto[]>([]);
  ngOnInit(): void {
    this.api.get<PagedResult<NotificationDto> | NotificationDto[]>('/admin/notifications').subscribe({
      next: (res) => this.items.set(Array.isArray(res) ? res : res.items ?? []),
    });
  }
  mark(n: NotificationDto): void {
    if (n.isRead) return;
    this.api.post(\`/admin/notifications/\${n.id}/read\`).subscribe({
      next: () => this.items.update((list) => list.map((x) => x.id === n.id ? { ...x, isRead: true } : x)),
    });
  }
}
`);

console.log('admin done');
