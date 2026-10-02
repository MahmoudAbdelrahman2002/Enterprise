const fs = require('fs');
const path = require('path');
const root = path.join('frontend', 'subito-web', 'src', 'app', 'features');
const write = (rel, content) => {
  const p = path.join(root, rel);
  fs.mkdirSync(path.dirname(p), { recursive: true });
  fs.writeFileSync(p, content);
  console.log(rel);
};

write('provider/products/provider-products.component.ts', `import { DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CategoryDetailDto, PagedResult, ProductDetailDto, ProductListItemDto } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-provider-products',
  standalone: true,
  imports: [FormsModule, DecimalPipe, EmptyStateComponent, PaginationComponent, TranslatePipe],
  template: \`
    <div class="toolbar">
      <h1 class="page-title">{{ 'nav.products' | t }}</h1>
      @if (canCreate) { <button class="btn btn-primary" type="button" (click)="showForm=true">{{ 'actions.create' | t }}</button> }
    </div>
    @if (showForm) {
      <form class="card stack" (ngSubmit)="create()">
        <div class="field"><label>Category</label>
          <select [(ngModel)]="form.categoryId" name="categoryId" required>
            @for (c of categories(); track c.id) { <option [value]="c.id">{{ c.name }}</option> }
          </select>
        </div>
        <div class="field"><label>Name (EN)</label><input [(ngModel)]="form.nameEn" name="nameEn" required /></div>
        <div class="field"><label>SKU</label><input [(ngModel)]="form.sku" name="sku" required /></div>
        <div class="field"><label>Price</label><input type="number" step="0.01" [(ngModel)]="form.price" name="price" required /></div>
        <div class="field"><label>Status</label>
          <select [(ngModel)]="form.status" name="status">
            <option [ngValue]="0">Draft</option><option [ngValue]="1">Active</option><option [ngValue]="2">Inactive</option>
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
        <thead><tr><th>Name</th><th>SKU</th><th>Price</th><th>Status</th><th></th></tr></thead>
        <tbody>
          @for (p of items(); track p.id) {
            <tr>
              <td>{{ p.name }}</td><td>{{ p.sku }}</td><td>{{ p.price | number:'1.2-2' }}</td><td>{{ p.status }}</td>
              <td class="row">
                @if (canUpdate) { <input type="file" (change)="upload(p, $event)" /> }
                @if (canDelete) { <button class="btn btn-danger" type="button" (click)="remove(p)">{{ 'actions.delete' | t }}</button> }
              </td>
            </tr>
          }
        </tbody>
      </table></div>
      <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" (change)="load($event)" />
    }
  \`,
})
export class ProviderProductsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  readonly items = signal<ProductListItemDto[]>([]);
  readonly categories = signal<CategoryDetailDto[]>([]);
  showForm = false; page = 1; totalPages = 1; totalCount = 0;
  form = { categoryId: '', nameEn: '', sku: '', price: 0, status: 1 };
  canCreate = this.tokens.hasPermission('provider', 'ProviderProduct.Create');
  canUpdate = this.tokens.hasPermission('provider', 'ProviderProduct.Update');
  canDelete = this.tokens.hasPermission('provider', 'ProviderProduct.Delete');

  ngOnInit(): void {
    this.api.get<CategoryDetailDto[] | PagedResult<CategoryDetailDto>>('/provider/categories/lookup').subscribe({
      next: (res) => this.categories.set(Array.isArray(res) ? res : res.items ?? []),
      error: () => {
        this.api.get<PagedResult<CategoryDetailDto>>('/provider/categories', { pageSize: 100 }).subscribe({
          next: (r) => this.categories.set(r.items ?? []),
        });
      },
    });
    this.load(1);
  }
  load(page: number): void {
    this.page = page;
    this.api.get<PagedResult<ProductListItemDto>>('/provider/products', { pageNumber: page, pageSize: 20 }).subscribe({
      next: (r) => { this.items.set(r.items ?? []); this.totalPages = r.totalPages; this.totalCount = r.totalCount; },
    });
  }
  create(): void {
    this.api.post<ProductDetailDto>(\`/provider/categories/\${this.form.categoryId}/products\`, {
      name: { en: this.form.nameEn },
      sku: this.form.sku,
      price: Number(this.form.price),
      status: this.form.status,
    }).subscribe({ next: () => { this.toast.success('Created'); this.showForm = false; this.load(1); } });
  }
  remove(p: ProductListItemDto): void {
    this.api.delete(\`/provider/categories/\${p.categoryId}/products/\${p.id}\`).subscribe({ next: () => this.load(this.page) });
  }
  upload(p: ProductListItemDto, ev: Event): void {
    const file = (ev.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.api.upload(\`/provider/categories/\${p.categoryId}/products/\${p.id}/image\`, file).subscribe({
      next: () => this.toast.success('Uploaded'),
    });
  }
}
`);

write('provider/orders/provider-orders.component.ts', `import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ORDER_STATUS_LABELS, OrderStatus } from '../../../core/models/api.models';
import { OrderDetailDto, OrderListItemDto, PagedResult } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-provider-orders',
  standalone: true,
  imports: [FormsModule, RouterLink, DatePipe, DecimalPipe, EmptyStateComponent, PaginationComponent, TranslatePipe],
  template: \`
    <h1 class="page-title">{{ 'nav.orders' | t }}</h1>
    @if (!selected()) {
      @if (!items().length) { <app-empty-state /> } @else {
        <div class="table-wrap card"><table class="data">
          <thead><tr><th>Date</th><th>Total</th><th>Status</th><th></th></tr></thead>
          <tbody>
            @for (o of items(); track o.id) {
              <tr>
                <td>{{ o.orderDateUtc | date:'medium' }}</td>
                <td>{{ o.totalAmount | number:'1.2-2' }}</td>
                <td>{{ labels[o.status] }}</td>
                <td><a [routerLink]="['/provider/orders', o.id]">View</a></td>
              </tr>
            }
          </tbody>
        </table></div>
        <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" (change)="load($event)" />
      }
    } @else {
      <div class="card stack">
        <a routerLink="/provider/orders">{{ 'actions.back' | t }}</a>
        <div class="row" style="justify-content:space-between">
          <span>{{ selected()!.orderDateUtc | date:'medium' }}</span>
          <span class="badge">{{ labels[selected()!.status] }}</span>
        </div>
        @for (i of selected()!.items; track i.id) {
          <div class="row" style="justify-content:space-between"><span>{{ i.productName }} × {{ i.quantity }}</span><span>{{ i.lineTotal | number:'1.2-2' }}</span></div>
        }
        <strong>{{ selected()!.totalAmount | number:'1.2-2' }}</strong>
        @if (canUpdate) {
          <div class="row">
            <select [(ngModel)]="nextStatus" name="nextStatus">
              @for (s of nextStatuses; track s) { <option [ngValue]="s">{{ labels[s] }}</option> }
            </select>
            <button class="btn btn-primary" type="button" (click)="updateStatus()">Update status</button>
          </div>
        }
      </div>
    }
  \`,
})
export class ProviderOrdersComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  readonly items = signal<OrderListItemDto[]>([]);
  readonly selected = signal<OrderDetailDto | null>(null);
  readonly labels = ORDER_STATUS_LABELS;
  page = 1; totalPages = 1; totalCount = 0; nextStatus = OrderStatus.Accepted;
  canUpdate = this.tokens.hasPermission('provider', 'ProviderOrder.Update');
  nextStatuses = [OrderStatus.Accepted, OrderStatus.Preparing, OrderStatus.Ready, OrderStatus.Completed, OrderStatus.Cancelled];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('orderId');
    if (id) {
      this.api.get<OrderDetailDto>(\`/provider/orders/\${id}\`).subscribe({ next: (o) => { this.selected.set(o); this.nextStatus = o.status as OrderStatus; } });
    } else {
      this.load(1);
    }
  }
  load(page: number): void {
    this.page = page;
    this.api.get<PagedResult<OrderListItemDto>>('/provider/orders', { pageNumber: page, pageSize: 20 }).subscribe({
      next: (r) => { this.items.set(r.items ?? []); this.totalPages = r.totalPages; this.totalCount = r.totalCount; },
    });
  }
  updateStatus(): void {
    const id = this.selected()?.id;
    if (!id) return;
    this.api.patch(\`/provider/orders/\${id}/status\`, { status: this.nextStatus }).subscribe({
      next: () => { this.toast.success('Updated'); this.ngOnInit(); },
    });
  }
}
`);

write('provider/roles/provider-roles.component.ts', `import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PermissionGroupDto, RoleDetailDto, RoleListItemDto, PagedResult } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-provider-roles',
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
          <fieldset>
            <legend>{{ g.module }}</legend>
            @for (p of g.permissions; track p.name) {
              <label style="display:block;margin:.25rem 0">
                <input type="checkbox" [checked]="selected.has(p.name)" (change)="toggle(p.name, $event)" [disabled]="editing()!.isSystem" />
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
export class ProviderRolesComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  readonly items = signal<RoleListItemDto[]>([]);
  readonly groups = signal<PermissionGroupDto[]>([]);
  readonly editing = signal<RoleDetailDto | null>(null);
  name = '';
  selected = new Set<string>();
  canCreate = this.tokens.hasPermission('provider', 'ProviderRoles.Create');
  canDelete = this.tokens.hasPermission('provider', 'ProviderRoles.Delete');

  ngOnInit(): void {
    this.api.get<PermissionGroupDto[]>('/provider/permissions').subscribe({ next: (g) => this.groups.set(g ?? []) });
    this.reload();
  }
  reload(): void {
    this.api.get<PagedResult<RoleListItemDto>>('/provider/roles', { pageSize: 50 }).subscribe({
      next: (r) => this.items.set(r.items ?? []),
    });
  }
  startCreate(): void {
    this.editing.set({ id: '', name: '', roleType: 'Provider', providerId: null, isSystem: false, usersCount: 0, permissions: [] });
    this.name = ''; this.selected = new Set();
  }
  edit(r: RoleListItemDto): void {
    this.api.get<RoleDetailDto>(\`/provider/roles/\${r.id}\`).subscribe({
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
    const req = current.id
      ? this.api.put(\`/provider/roles/\${current.id}\`, body)
      : this.api.post('/provider/roles', body);
    req.subscribe({ next: () => { this.toast.success('Saved'); this.editing.set(null); this.reload(); } });
  }
  remove(id: string): void {
    this.api.delete(\`/provider/roles/\${id}\`).subscribe({ next: () => this.reload() });
  }
}
`);

write('provider/staff/provider-staff.component.ts', `import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PagedResult, RoleListItemDto, StaffDetailDto, StaffListItemDto } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-provider-staff',
  standalone: true,
  imports: [FormsModule, EmptyStateComponent, TranslatePipe],
  template: \`
    <div class="toolbar">
      <h1 class="page-title">{{ 'nav.staff' | t }}</h1>
      @if (canCreate) { <button class="btn btn-primary" type="button" (click)="showForm=true">{{ 'actions.create' | t }}</button> }
    </div>
    @if (showForm) {
      <form class="card stack" (ngSubmit)="create()">
        <div class="field"><label>{{ 'auth.firstName' | t }}</label><input [(ngModel)]="form.firstName" name="firstName" /></div>
        <div class="field"><label>{{ 'auth.lastName' | t }}</label><input [(ngModel)]="form.lastName" name="lastName" /></div>
        <div class="field"><label>{{ 'auth.email' | t }}</label><input [(ngModel)]="form.email" name="email" /></div>
        <div class="field"><label>{{ 'auth.password' | t }}</label><input type="password" [(ngModel)]="form.password" name="password" /></div>
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
          @for (s of items(); track s.id) {
            <tr>
              <td>{{ s.firstName }} {{ s.lastName }}</td><td>{{ s.email }}</td><td>{{ s.roleName }}</td>
              <td>{{ s.isActive }}</td>
              <td class="row">
                @if (canUpdate && !s.isSystem) {
                  <button class="btn btn-ghost" type="button" (click)="toggle(s)">{{ s.isActive ? ('actions.deactivate'|t) : ('actions.activate'|t) }}</button>
                }
                @if (canDelete && !s.isSystem) {
                  <button class="btn btn-danger" type="button" (click)="remove(s.id)">{{ 'actions.delete' | t }}</button>
                }
              </td>
            </tr>
          }
        </tbody>
      </table></div>
    }
  \`,
})
export class ProviderStaffComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  readonly items = signal<StaffListItemDto[]>([]);
  readonly roles = signal<RoleListItemDto[]>([]);
  showForm = false;
  form = { firstName: '', lastName: '', email: '', password: '', roleId: '', phoneNumber: '' };
  canCreate = this.tokens.hasPermission('provider', 'ProviderStaff.Create');
  canUpdate = this.tokens.hasPermission('provider', 'ProviderStaff.Update');
  canDelete = this.tokens.hasPermission('provider', 'ProviderStaff.Delete');

  ngOnInit(): void {
    this.api.get<PagedResult<RoleListItemDto>>('/provider/roles', { pageSize: 100 }).subscribe({
      next: (r) => this.roles.set(r.items ?? []),
    });
    this.reload();
  }
  reload(): void {
    this.api.get<PagedResult<StaffListItemDto>>('/provider/staff', { pageSize: 50 }).subscribe({
      next: (r) => this.items.set(r.items ?? []),
    });
  }
  create(): void {
    this.api.post<StaffDetailDto>('/provider/staff', this.form).subscribe({
      next: () => { this.toast.success('Created'); this.showForm = false; this.reload(); },
    });
  }
  toggle(s: StaffListItemDto): void {
    this.api.post(\`/provider/staff/\${s.id}/set-active\`, { isActive: !s.isActive }).subscribe({ next: () => this.reload() });
  }
  remove(id: string): void {
    this.api.delete(\`/provider/staff/\${id}\`).subscribe({ next: () => this.reload() });
  }
}
`);

write('provider/profile/provider-profile.component.ts', `import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ProfileDto } from '../../../core/models/api.models';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-provider-profile',
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
export class ProviderProfileComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  readonly profile = signal<ProfileDto | null>(null);
  firstName = ''; lastName = ''; currentPassword = ''; newPassword = '';
  ngOnInit(): void {
    this.auth.getProfile('provider').subscribe({ next: (p) => { this.profile.set(p); this.firstName = p.firstName; this.lastName = p.lastName; } });
  }
  save(): void {
    this.auth.updateProfile('provider', { firstName: this.firstName, lastName: this.lastName }).subscribe({
      next: (p) => { this.profile.set(p); this.toast.success('Saved'); },
    });
  }
  changePassword(): void {
    this.auth.changePassword('provider', { currentPassword: this.currentPassword, newPassword: this.newPassword }).subscribe({
      next: () => this.toast.success('Password changed'),
    });
  }
}
`);

write('provider/notifications/provider-notifications.component.ts', `import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { NotificationDto, PagedResult } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-provider-notifications',
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
export class ProviderNotificationsComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly items = signal<NotificationDto[]>([]);
  ngOnInit(): void {
    this.api.get<PagedResult<NotificationDto> | NotificationDto[]>('/provider/notifications').subscribe({
      next: (res) => this.items.set(Array.isArray(res) ? res : res.items ?? []),
    });
  }
  mark(n: NotificationDto): void {
    if (n.isRead) return;
    this.api.post(\`/provider/notifications/\${n.id}/read\`).subscribe({
      next: () => this.items.update((list) => list.map((x) => x.id === n.id ? { ...x, isRead: true } : x)),
    });
  }
}
`);

console.log('provider features done');
