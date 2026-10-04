import { FieldValidationDirective } from '../../../shared/directives/field-validation.directive';
import { fieldRules } from '../../../shared/forms/field-validators';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { finalize } from 'rxjs';
import { ImageUploadComponent } from '../../../shared/components/image-upload/image-upload.component';
import { I18nService } from '../../../core/services/i18n.service';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MarketplaceServiceLookupDto, ProviderAdminDto } from '../../../core/models/domain.models';
import { CatalogService } from '../../../core/services/catalog.service';
import { ProvidersService } from '../../../core/services/providers.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { readList } from '../../../core/utils/read-list';
import { applyServerError, firstErrorKey } from '../../../shared/forms/form-errors';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-provider-detail',
  standalone: true,
  imports: [FieldValidationDirective, IconComponent, ImageUploadComponent, ReactiveFormsModule, RouterLink, TranslatePipe],
  template: `
    <div class="toolbar"><h1 class="page-title">{{ 'nav.provider' | t }}</h1><a routerLink="/admin/providers" class="btn btn-ghost icon-action" [attr.aria-label]="'actions.back' | t" [title]="'actions.back' | t"><app-icon name="left" /></a></div>
    @if (loading()) { <p class="muted">{{ 'loading' | t }}</p> }
    @else if (provider()) {
      <form class="card stack" [formGroup]="form" (ngSubmit)="save()">
        @if (form.errors?.['server']) { <p class="field-error" role="alert">{{ form.errors?.['server'] }}</p> }
        @if (provider()!.imageUrl) { <img class="thumb-lg" [src]="provider()!.imageUrl!" [alt]="provider()!.companyName" /> }
        <div class="field"><label for="admin-provider-detail-companyName">{{ 'ui.companyName' | t }}</label><input id="admin-provider-detail-companyName" appFieldValidation formControlName="companyName" /></div>
        <div class="field"><label for="admin-provider-detail-firstName">{{ 'auth.firstName' | t }}</label><input id="admin-provider-detail-firstName" appFieldValidation formControlName="firstName" /></div>
        <div class="field"><label for="admin-provider-detail-lastName">{{ 'auth.lastName' | t }}</label><input id="admin-provider-detail-lastName" appFieldValidation formControlName="lastName" /></div>
        <div class="field"><label for="admin-provider-detail-phoneNumber">{{ 'ui.phone' | t }}</label><input id="admin-provider-detail-phoneNumber" appFieldValidation formControlName="phoneNumber" /></div>
        @if (canUpdate && canReadServices) {
        <div class="field"><label for="admin-provider-detail-serviceId">{{ 'ui.service' | t }}</label><select id="admin-provider-detail-serviceId" appFieldValidation formControlName="serviceId">
            @for (s of services(); track s.id) { <option [value]="s.id">{{ s.name }}</option> }
          </select>
        </div>
        } @else {
          <p class="muted">{{ 'ui.service' | t }}: {{ provider()!.serviceName }}</p>
        }
        <p class="muted">{{ provider()!.email }}</p>
        @if (canUpdate) {
          <app-image-upload [disabled]="busy()" (selected)="upload($event)" />
          <button class="btn btn-primary icon-action" type="submit" [disabled]="busy()" [attr.aria-label]="'actions.save' | t" [title]="'actions.save' | t"><app-icon name="check" />{{ 'actions.save' | t }}</button>
        }
      </form>
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
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly form = inject(FormBuilder).nonNullable.group({
    firstName: ['', fieldRules.name],
    lastName: ['', fieldRules.name],
    companyName: ['', fieldRules.company],
    phoneNumber: ['', fieldRules.phone],
    serviceId: ['', fieldRules.optionalId],
  });
  canUpdate = this.tokens.hasPermission('admin', 'Providers.Update');
  readonly canReadServices = this.tokens.hasPermission('admin', 'Services.Read');
  id = '';

  ngOnInit(): void {
    this.id = this.route.snapshot.paramMap.get('id')!;
    if (!this.canUpdate) this.form.disable();
    if (this.canUpdate && this.canReadServices) {
      this.catalog.lookupAdminServices().subscribe({
        next: (s) => this.services.set(readList<MarketplaceServiceLookupDto>(s)),
        error: () => this.services.set([]),
      });
    }
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
  }

  error(name: 'companyName' | 'firstName' | 'lastName'): string | null {
    return firstErrorKey(this.form.controls[name]);
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.busy.set(true);
    const value = this.form.getRawValue();
    this.providers.update(this.id, { ...value, serviceId: value.serviceId || null }).subscribe({
      next: (p) => { this.provider.set(p); this.toast.success(this.i18n.t('ui.saved')); this.busy.set(false); },
      error: (err) => { applyServerError(this.form, err); this.busy.set(false); },
    });
  }

  upload(file: File): void {
    if (!file || this.busy()) return;
    this.busy.set(true);
    this.providers.uploadImage(this.id, file).pipe(finalize(() => this.busy.set(false))).subscribe({ next: (provider) => { this.provider.set(provider); this.toast.success(this.i18n.t('ui.uploaded')); } });
  }
}
