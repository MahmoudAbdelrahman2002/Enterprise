import { I18nService } from '../../../core/services/i18n.service';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { finalize } from 'rxjs';
import { Component, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { MarketplaceServiceLookupDto, ProviderAdminDto } from '../../../core/models/domain.models';
import { ConfirmService } from '../../../core/services/confirm.service';
import { ProvidersService } from '../../../core/services/providers.service';
import { CatalogService } from '../../../core/services/catalog.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { readList, readPage } from '../../../core/utils/read-list';
import { applyServerError, firstErrorKey, strongPassword } from '../../../shared/forms/form-errors';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { SearchFieldComponent } from '../../../shared/components/search-field/search-field.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-providers',
  standalone: true,
  imports: [IconComponent, ReactiveFormsModule, RouterLink, EmptyStateComponent, PaginationComponent, SearchFieldComponent, TranslatePipe],
  template: `
    <div class="toolbar">
      <h1 class="page-title">{{ 'nav.providers' | t }}</h1>
      <div class="row">
        <app-search-field [control]="search" placeholderKey="search.providers" />
        @if (canCreate) { <button class="btn btn-primary" type="button" (click)="showForm.set(true)">{{ 'actions.create' | t }}</button> }
      </div>
    </div>
    @if (showForm()) {
      <form class="card stack" [formGroup]="form" (ngSubmit)="create()">
        <div class="field"><label for="admin-providers-email">{{ 'auth.email' | t }}</label><input id="admin-providers-email" type="email" formControlName="email" autocomplete="email" />
          @if (error('email'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="admin-providers-password">{{ 'auth.password' | t }}</label><input id="admin-providers-password" type="password" formControlName="password" autocomplete="current-password" />
          @if (error('password'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="admin-providers-firstName">{{ 'auth.firstName' | t }}</label><input id="admin-providers-firstName" formControlName="firstName" />
          @if (error('firstName'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="admin-providers-lastName">{{ 'auth.lastName' | t }}</label><input id="admin-providers-lastName" formControlName="lastName" />
          @if (error('lastName'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="admin-providers-companyName">{{ 'ui.company' | t }}</label><input id="admin-providers-companyName" formControlName="companyName" />
          @if (error('companyName'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="admin-providers-phoneNumber">{{ 'ui.phone' | t }}</label><input id="admin-providers-phoneNumber" formControlName="phoneNumber" /></div>
        <div class="field"><label for="admin-providers-serviceId">{{ 'ui.service' | t }}</label><select id="admin-providers-serviceId" formControlName="serviceId">
            <option value="">—</option>
            @for (s of services(); track s.id) { <option [value]="s.id">{{ s.name }}</option> }
          </select>
        </div>
        @if (form.errors?.['server']) { <p class="field-error">{{ form.errors?.['server'] }}</p> }
        <div class="row">
          <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'actions.save' | t }}</button>
          <button class="btn btn-ghost" type="button" (click)="showForm.set(false)">{{ 'actions.cancel' | t }}</button>
        </div>
      </form>
    }
    @if (loading()) { <p class="muted">{{ 'loading' | t }}</p> }
    @else if (failed()) { <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost" type="button" (click)="load(1)">{{ 'actions.retry' | t }}</button></div> } @else if (!items().length) { <app-empty-state /> } @else {
      <div class="table-wrap card"><table class="data">
        <thead><tr><th>{{ 'ui.company' | t }}</th><th>{{ 'auth.email' | t }}</th><th>{{ 'ui.service' | t }}</th><th>{{ 'ui.active' | t }}</th><th></th></tr></thead>
        <tbody>
          @for (p of items(); track p.id) {
            <tr>
              <td>{{ p.companyName }}</td><td>{{ p.email }}</td><td>{{ p.serviceName }}</td>
              <td>
                <span class="badge" [class.badge-success]="asBool(p.isActive)" [class.badge-danger]="!asBool(p.isActive)">
                  {{ asBool(p.isActive) ? ('status.active'|t) : ('status.inactive'|t) }}
                </span>
              </td>
              <td class="row">
                <a [routerLink]="['/admin/providers', p.id]">{{ 'actions.view' | t }}</a>
                @if (canUpdate) {
                  <button
                    class="btn btn-ghost"
                    type="button"
                    [disabled]="togglingId() === p.id"
                    (click)="toggle(p)"
                  >
                    {{ asBool(p.isActive) ? ('actions.deactivate'|t) : ('actions.activate'|t) }}
                  </button>
                }
                @if (canDelete) { <button class="btn btn-danger" type="button" [disabled]="busy()" (click)="remove(p.id)"><app-icon name="trash" />{{ 'actions.delete' | t }}</button> }
              </td>
            </tr>
          }
        </tbody>
      </table></div>
      <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" (change)="load($event)" />
    }
  `,
})
export class AdminProvidersComponent implements OnInit {
  private readonly i18n = inject(I18nService);
  private readonly providers = inject(ProvidersService);
  private readonly catalog = inject(CatalogService);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  private readonly confirm = inject(ConfirmService);
  readonly items = signal<ProviderAdminDto[]>([]);
  readonly services = signal<MarketplaceServiceLookupDto[]>([]);
  readonly showForm = signal(false);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly busy = signal(false);
  readonly togglingId = signal<string | null>(null);
  readonly search = new FormControl('', { nonNullable: true });
  page = 1; totalPages = 1; totalCount = 0;
  readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, strongPassword]],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    companyName: ['', Validators.required],
    phoneNumber: [''],
    serviceId: [''],
  });
  canCreate = this.tokens.hasPermission('admin', 'Providers.Create');
  canUpdate = this.tokens.hasPermission('admin', 'Providers.Update');
  canDelete = this.tokens.hasPermission('admin', 'Providers.Delete');

  constructor() {
    this.search.valueChanges.pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed()).subscribe(() => this.load(1));
  }

  ngOnInit(): void {
    this.catalog.lookupAdminServices().subscribe({
      next: (s) => this.services.set(readList<MarketplaceServiceLookupDto>(s)),
    });
    this.load(1);
  }

  error(name: 'email' | 'password' | 'firstName' | 'lastName' | 'companyName'): string | null {
    return firstErrorKey(this.form.controls[name]);
  }

  load(page: number): void {
    this.page = page;
    this.loading.set(true); this.failed.set(false);
    this.providers.list({ pageNumber: page, pageSize: 20, searchTerm: this.search.value || null }).subscribe({
      next: (r) => {
        const pageData = readPage<ProviderAdminDto>(r);
        this.items.set(pageData.items.map((p) => this.normalizeProvider(p)));
        this.totalPages = pageData.totalPages;
        this.totalCount = pageData.totalCount;
        this.loading.set(false);
      },
      error: () => { this.failed.set(true); this.loading.set(false); },
    });
  }

  asBool(value: unknown): boolean {
    if (typeof value === 'boolean') return value;
    if (typeof value === 'number') return value !== 0;
    if (typeof value === 'string') {
      const normalized = value.trim().toLowerCase();
      return normalized === 'true' || normalized === '1';
    }
    return false;
  }

  private normalizeProvider(p: ProviderAdminDto): ProviderAdminDto {
    const raw = p as ProviderAdminDto & { IsActive?: unknown };
    return { ...p, isActive: this.asBool(raw.isActive ?? raw.IsActive) };
  }

  create(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.busy.set(true);
    const value = this.form.getRawValue();
    this.providers.create({ ...value, serviceId: value.serviceId || null }).subscribe({
      next: () => {
        this.toast.success(this.i18n.t('ui.created'));
        this.showForm.set(false);
        this.form.reset();
        this.busy.set(false);
        this.load(1);
      },
      error: (err) => { applyServerError(this.form, err); this.busy.set(false); },
    });
  }

  toggle(p: ProviderAdminDto): void {
    if (this.togglingId()) return;
    const next = !this.asBool(p.isActive);
    this.togglingId.set(p.id);
    this.providers.setActive(p.id, next).subscribe({
      next: () => {
        this.items.update((list) =>
          list.map((x) => (x.id === p.id ? { ...x, isActive: next } : x))
        );
        this.toast.success(this.i18n.t(next ? 'status.active' : 'status.inactive'));
        this.togglingId.set(null);
      },
      error: () => this.togglingId.set(null),
    });
  }

  async remove(id: string): Promise<void> {
    if (this.busy()) return;
    if (!(await this.confirm.ask())) return;
    this.busy.set(true);
    this.providers.remove(id).pipe(finalize(() => this.busy.set(false))).subscribe({ next: () => this.load(this.page) });
  }
}
