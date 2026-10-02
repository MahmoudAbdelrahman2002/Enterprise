import { finalize } from 'rxjs';
import { ImageUploadComponent } from '../../../shared/components/image-upload/image-upload.component';
import { I18nService } from '../../../core/services/i18n.service';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CategoryDetailDto, MarketplaceServiceLookupDto, ProductListItemDto, ProviderAdminDto } from '../../../core/models/domain.models';
import { CatalogService } from '../../../core/services/catalog.service';
import { ProvidersService } from '../../../core/services/providers.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { readList } from '../../../core/utils/read-list';
import { firstErrorKey } from '../../../shared/forms/form-errors';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-provider-detail',
  standalone: true,
  imports: [ImageUploadComponent, ReactiveFormsModule, RouterLink, TranslatePipe],
  template: `
    <div class="toolbar"><h1 class="page-title">{{ 'nav.provider' | t }}</h1><a routerLink="/admin/providers" class="btn btn-ghost">{{ 'actions.back' | t }}</a></div>
    @if (loading()) { <p class="muted">{{ 'loading' | t }}</p> }
    @else if (provider()) {
      <form class="card stack" [formGroup]="form" (ngSubmit)="save()">
        @if (provider()!.imageUrl) { <img class="thumb-lg" [src]="provider()!.imageUrl!" alt="" /> }
        <div class="field"><label for="admin-provider-detail-companyName">{{ 'ui.company' | t }}</label><input id="admin-provider-detail-companyName" formControlName="companyName" />
          @if (error('companyName'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="admin-provider-detail-firstName">{{ 'auth.firstName' | t }}</label><input id="admin-provider-detail-firstName" formControlName="firstName" />
          @if (error('firstName'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="admin-provider-detail-lastName">{{ 'auth.lastName' | t }}</label><input id="admin-provider-detail-lastName" formControlName="lastName" />
          @if (error('lastName'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="admin-provider-detail-phoneNumber">{{ 'ui.phone' | t }}</label><input id="admin-provider-detail-phoneNumber" formControlName="phoneNumber" /></div>
        <div class="field"><label for="admin-provider-detail-serviceId">{{ 'ui.service' | t }}</label><select id="admin-provider-detail-serviceId" formControlName="serviceId">
            @for (s of services(); track s.id) { <option [value]="s.id">{{ s.name }}</option> }
          </select>
        </div>
        <p class="muted">{{ provider()!.email }}</p>
        @if (canUpdate) {
          <app-image-upload [disabled]="busy()" (selected)="upload($event)" />
          <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'actions.save' | t }}</button>
        }
      </form>
      <div class="card" style="margin-top:1rem">
        <h3>{{ 'nav.categories' | t }}</h3>
        <ul>@for (c of categories(); track c.id) { <li>{{ c.name }} ({{ (c.isActive ? 'status.active' : 'status.inactive') | t }})</li> }</ul>
        <h3>{{ 'nav.products' | t }}</h3>
        <ul>@for (p of products(); track p.id) { <li>{{ p.name }} — {{ p.sku }}</li> }</ul>
      </div>
    }
  `,
})
export class AdminProviderDetailComponent implements OnInit {
  private readonly i18n = inject(I18nService);
  private readonly providers = inject(ProvidersService);
  private readonly catalog = inject(CatalogService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  readonly provider = signal<ProviderAdminDto | null>(null);
  readonly services = signal<MarketplaceServiceLookupDto[]>([]);
  readonly categories = signal<CategoryDetailDto[]>([]);
  readonly products = signal<ProductListItemDto[]>([]);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly form = inject(FormBuilder).nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    companyName: ['', Validators.required],
    phoneNumber: [''],
    serviceId: [''],
  });
  canUpdate = this.tokens.hasPermission('admin', 'Providers.Update');
  id = '';

  ngOnInit(): void {
    this.id = this.route.snapshot.paramMap.get('id')!;
    if (!this.canUpdate) this.form.disable();
    this.catalog.lookupAdminServices().subscribe({
      next: (s) => this.services.set(readList<MarketplaceServiceLookupDto>(s)),
    });
    this.providers.get(this.id).subscribe({
      next: (p) => {
        this.provider.set(p);
        this.form.patchValue({
          firstName: p.firstName,
          lastName: p.lastName,
          companyName: p.companyName,
          phoneNumber: p.phoneNumber || '',
          serviceId: p.serviceId || '',
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
    this.providers.listCategories(this.id).subscribe({
      next: (c) => this.categories.set(readList<CategoryDetailDto>(c)),
    });
    this.providers.listProducts(this.id).subscribe({
      next: (p) => this.products.set(readList<ProductListItemDto>(p)),
    });
  }

  error(name: 'companyName' | 'firstName' | 'lastName'): string | null {
    return firstErrorKey(this.form.controls[name]);
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.busy.set(true);
    this.providers.update(this.id, this.form.getRawValue()).subscribe({
      next: (p) => { this.provider.set(p); this.toast.success(this.i18n.t('ui.saved')); this.busy.set(false); },
      error: () => this.busy.set(false),
    });
  }

  upload(ev: Event): void {
    const file = (ev.target as HTMLInputElement).files?.[0];
    if (!file || this.busy()) return;
    this.busy.set(true);
    this.providers.uploadImage(this.id, file).pipe(finalize(() => this.busy.set(false))).subscribe({ next: () => { this.toast.success(this.i18n.t('ui.uploaded')); this.ngOnInit(); } });
  }
}
