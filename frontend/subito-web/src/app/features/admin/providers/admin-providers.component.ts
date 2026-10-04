import { PageRequest } from '../../../core/utils/page-request';
import { FieldValidationDirective } from '../../../shared/directives/field-validation.directive';
import { fieldRules } from '../../../shared/forms/field-validators';
import { ActiveToggleComponent } from '../../../shared/components/active-toggle/active-toggle.component';
import { PasswordToggleDirective } from '../../../shared/directives/password-toggle.directive';
import { I18nService } from '../../../core/services/i18n.service';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { finalize } from 'rxjs';
import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
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
import { readList, readPage, resolvePage } from '../../../core/utils/read-list';
import { applyServerError, firstErrorKey, strongPassword } from '../../../shared/forms/form-errors';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { SearchFieldComponent } from '../../../shared/components/search-field/search-field.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-providers',
  standalone: true,
  imports: [FieldValidationDirective, ActiveToggleComponent, PasswordToggleDirective, IconComponent, ReactiveFormsModule, RouterLink, EmptyStateComponent, PaginationComponent, SearchFieldComponent, TranslatePipe],
  template: `
    <div class="toolbar">
      <h1 class="page-title">{{ 'nav.providers' | t }}</h1>
      <div class="row">
        @if (!showForm()) { <app-search-field [control]="search" placeholderKey="search.providers" /> }
        @if (canCreate && !showForm()) { <button class="btn btn-primary icon-action" type="button" (click)="showForm.set(true)" [attr.aria-label]="'actions.create' | t" [title]="'actions.create' | t"><app-icon name="plus" />{{ 'actions.create' | t }}</button> }
      </div>
    </div>
    <p class="muted">{{ 'ui.providerTerminology' | t }}</p>
    @if (showForm()) {
      <form class="card stack" [formGroup]="form" (ngSubmit)="create()">
        <div class="field"><label for="admin-providers-email">{{ 'auth.email' | t }}</label><input id="admin-providers-email" type="email" appFieldValidation formControlName="email" autocomplete="email" /></div>
        <div class="field"><label for="admin-providers-password">{{ 'auth.password' | t }}</label><input id="admin-providers-password" type="password" appPasswordToggle appFieldValidation formControlName="password" autocomplete="current-password" /></div>
        <div class="field"><label for="admin-providers-firstName">{{ 'auth.firstName' | t }}</label><input id="admin-providers-firstName" appFieldValidation formControlName="firstName" /></div>
        <div class="field"><label for="admin-providers-lastName">{{ 'auth.lastName' | t }}</label><input id="admin-providers-lastName" appFieldValidation formControlName="lastName" /></div>
        <div class="field"><label for="admin-providers-companyName">{{ 'ui.companyName' | t }}</label><input id="admin-providers-companyName" appFieldValidation formControlName="companyName" /></div>
        <div class="field"><label for="admin-providers-phoneNumber">{{ 'ui.phone' | t }}</label><input id="admin-providers-phoneNumber" appFieldValidation formControlName="phoneNumber" /></div>
        @if (canReadServices) {
        <div class="field"><label for="admin-providers-serviceId">{{ 'ui.service' | t }}</label><select id="admin-providers-serviceId" appFieldValidation formControlName="serviceId">
            <option value="">—</option>
            @for (s of services(); track s.id) { <option [value]="s.id">{{ s.name }}</option> }
          </select>
        </div>
        }
        @if (form.errors?.['server']) { <p class="field-error">{{ form.errors?.['server'] }}</p> }
        <div class="row">
          <button class="btn btn-primary icon-action" type="submit" [disabled]="busy()" [attr.aria-label]="'actions.save' | t" [title]="'actions.save' | t"><app-icon name="check" />{{ 'actions.save' | t }}</button>
          <button class="btn btn-ghost icon-action" type="button" (click)="showForm.set(false)" [attr.aria-label]="'actions.cancel' | t" [title]="'actions.cancel' | t"><app-icon name="close" />{{ 'actions.cancel' | t }}</button>
        </div>
      </form>
    }
    @if (!showForm()) {
    @if (loading()) { <p class="muted">{{ 'loading' | t }}</p> }
    @else if (failed()) { <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost icon-action" type="button" (click)="load(1)" [attr.aria-label]="'actions.retry' | t" [title]="'actions.retry' | t"><app-icon name="retry" /></button></div> } @else if (!items().length) { <app-empty-state /> } @else {
      <div class="table-wrap card"><table class="data">
        <thead><tr><th>{{ 'ui.companyName' | t }}</th><th>{{ 'auth.email' | t }}</th><th>{{ 'ui.service' | t }}</th><th>{{ 'ui.active' | t }}</th><th></th></tr></thead>
        <tbody>
          @for (p of items(); track p.id) {
            <tr>
              <td><div class="provider-identity">@if (p.imageUrl) { <img class="thumb" [src]="p.imageUrl" [alt]="p.companyName" loading="lazy" /> } @else { <span class="thumb media-fallback">{{ p.companyName.slice(0, 1) }}</span> }<span>{{ p.companyName }}</span></div></td><td>{{ p.email }}</td><td>{{ p.serviceName }}</td>
              <td>@if (canUpdate) { <app-active-toggle [targetName]="p.companyName" confirmationKey="confirm.deactivateProvider" [active]="asBool(p.isActive)" [disabled]="busy() || togglingId() !== null" (changed)="toggle(p)" /> } @else { <span class="badge" [class.badge-success]="asBool(p.isActive)" [class.badge-danger]="!asBool(p.isActive)">
                  {{ asBool(p.isActive) ? ('status.active'|t) : ('status.inactive'|t) }}
                </span> }</td>
              <td class="row">
                <a [routerLink]="['/admin/providers', p.id]" [attr.aria-label]="'actions.view' | t" [title]="'actions.view' | t" class="btn btn-ghost icon-action"><app-icon name="eye" /></a>
                @if (canDelete) { <button class="btn btn-danger icon-action" type="button" [disabled]="busy()" (click)="remove(p.id)" [attr.aria-label]="'actions.delete' | t" [title]="'actions.delete' | t"><app-icon name="trash" /></button> }
              </td>
            </tr>
          }
        </tbody>
      </table></div>
    }
    }
    @if (!showForm()) { <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" [disabled]="loading() || busy()" (change)="load($event)" /> }
  `,
})
export class AdminProvidersComponent implements OnInit {
  private readonly pageRequest = new PageRequest(inject(DestroyRef));
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
  readonly search = new FormControl('', { nonNullable: true, validators: fieldRules.search });
  page = 1; totalPages = 1; totalCount = 0;
  readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', fieldRules.email],
    password: ['', fieldRules.password],
    firstName: ['', fieldRules.name],
    lastName: ['', fieldRules.name],
    companyName: ['', fieldRules.company],
    phoneNumber: ['', fieldRules.phone],
    serviceId: ['', fieldRules.optionalId],
  });
  canCreate = this.tokens.hasPermission('admin', 'Providers.Create');
  canUpdate = this.tokens.hasPermission('admin', 'Providers.Update');
  canDelete = this.tokens.hasPermission('admin', 'Providers.Delete');
  readonly canReadServices = this.tokens.hasPermission('admin', 'Services.Read');

  constructor() {
    this.search.valueChanges.pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed()).subscribe(() => { if (this.search.valid) this.load(1); });
  }

  ngOnInit(): void {
    if (this.canCreate && this.canReadServices) {
      this.catalog.lookupAdminServices().subscribe({
        next: (s) => this.services.set(readList<MarketplaceServiceLookupDto>(s)),
        error: () => this.services.set([]),
      });
    }
    this.load(1);
  }

  error(name: 'email' | 'password' | 'firstName' | 'lastName' | 'companyName'): string | null {
    return firstErrorKey(this.form.controls[name]);
  }

  load(page: number): void {
    this.page = page;
    this.loading.set(true); this.failed.set(false);
    this.pageRequest.run(this.providers.list({ pageNumber: page, pageSize: 20, searchTerm: this.search.value || null }), {
      next: (r) => {
        const pageData = readPage<ProviderAdminDto>(r);
        const targetPage = resolvePage(page, pageData);
        if (page !== targetPage) { this.load(targetPage); return; }
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
