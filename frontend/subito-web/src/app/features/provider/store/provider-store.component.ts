import { FieldValidationDirective } from '../../../shared/directives/field-validation.directive';
import { fieldRules } from '../../../shared/forms/field-validators';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { finalize } from 'rxjs';
import { ImageUploadComponent } from '../../../shared/components/image-upload/image-upload.component';
import { I18nService } from '../../../core/services/i18n.service';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProviderStoreDto } from '../../../core/models/domain.models';
import { StoreService } from '../../../core/services/store.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { ToastService } from '../../../core/services/toast.service';
import { applyServerError, firstErrorKey } from '../../../shared/forms/form-errors';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-provider-store',
  standalone: true,
  imports: [FieldValidationDirective, IconComponent, ImageUploadComponent, ReactiveFormsModule, TranslatePipe],
  template: `
    <h1 class="page-title">{{ 'nav.store' | t }}</h1>
    @if (loading()) { <p class="muted">{{ 'loading' | t }}</p> }
    @else if (store()) {
      <form class="card stack" [formGroup]="form" (ngSubmit)="save()">
        @if (form.errors?.['server']) { <p class="field-error" role="alert">{{ form.errors?.['server'] }}</p> }
        @if (store()!.imageUrl) { <img class="thumb-lg" [src]="store()!.imageUrl!" [alt]="store()!.companyName" /> }
        <div class="field"><label for="provider-store-companyName">{{ 'ui.company' | t }}</label><input id="provider-store-companyName" appFieldValidation formControlName="companyName" /></div>
        <div class="field"><label for="provider-store-phoneNumber">{{ 'ui.phone' | t }}</label><input id="provider-store-phoneNumber" appFieldValidation formControlName="phoneNumber" /></div>
        <p class="muted">{{ 'ui.service' | t }}: {{ store()!.serviceName }}</p>
        @if (canUpdate) {
          <app-image-upload [disabled]="busy()" (selected)="onFile($event)" />
          <button class="btn btn-primary icon-action" type="submit" [disabled]="busy()" [attr.aria-label]="'actions.save' | t" [title]="'actions.save' | t"><app-icon name="check" />{{ 'actions.save' | t }}</button>
        }
      </form>
    }
  `,
})
export class ProviderStoreComponent implements OnInit {
  private readonly i18n = inject(I18nService);
  private readonly storeApi = inject(StoreService);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  readonly canUpdate = this.tokens.hasPermission('provider', 'ProviderStore.Update');
  readonly store = signal<ProviderStoreDto | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly form = inject(FormBuilder).nonNullable.group({
    companyName: ['', fieldRules.company],
    phoneNumber: ['', fieldRules.phone],
  });

  ngOnInit(): void {
    if (!this.canUpdate) this.form.disable();
    this.loading.set(true);
    this.storeApi.get().subscribe({
      next: (s) => {
        this.store.set(s);
        this.form.patchValue({ companyName: s.companyName, phoneNumber: s.phoneNumber || '' });
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  nameError(): string | null { return firstErrorKey(this.form.controls.companyName); }

  save(): void {
    if (!this.canUpdate || this.busy()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.busy.set(true);
    const value = this.form.getRawValue();
    this.storeApi.update(value.companyName, value.phoneNumber).subscribe({
      next: (s) => { this.store.set(s); this.toast.success(this.i18n.t('ui.saved')); this.busy.set(false); },
      error: (err) => { applyServerError(this.form, err); this.busy.set(false); },
    });
  }

  onFile(file: File): void {
    if (!this.canUpdate || !file || this.busy()) return;
    this.busy.set(true);
    this.storeApi.uploadImage(file).pipe(finalize(() => this.busy.set(false))).subscribe({ next: () => { this.toast.success(this.i18n.t('ui.uploaded')); this.ngOnInit(); } });
  }
}
