import { finalize } from 'rxjs';
import { ImageUploadComponent } from '../../../shared/components/image-upload/image-upload.component';
import { I18nService } from '../../../core/services/i18n.service';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProviderStoreDto } from '../../../core/models/domain.models';
import { StoreService } from '../../../core/services/store.service';
import { ToastService } from '../../../core/services/toast.service';
import { firstErrorKey } from '../../../shared/forms/form-errors';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-provider-store',
  standalone: true,
  imports: [ImageUploadComponent, ReactiveFormsModule, TranslatePipe],
  template: `
    <h1 class="page-title">{{ 'nav.store' | t }}</h1>
    @if (loading()) { <p class="muted">{{ 'loading' | t }}</p> }
    @else if (store()) {
      <form class="card stack" [formGroup]="form" (ngSubmit)="save()">
        @if (store()!.imageUrl) { <img class="thumb-lg" [src]="store()!.imageUrl!" [alt]="store()!.companyName" /> }
        <div class="field"><label for="provider-store-companyName">{{ 'ui.company' | t }}</label><input id="provider-store-companyName" formControlName="companyName" />
          @if (nameError(); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="provider-store-phoneNumber">{{ 'ui.phone' | t }}</label><input id="provider-store-phoneNumber" formControlName="phoneNumber" /></div>
        <p class="muted">{{ 'ui.service' | t }}: {{ store()!.serviceName }}</p>
        <app-image-upload [disabled]="busy()" (selected)="onFile($event)" />
        <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'actions.save' | t }}</button>
      </form>
    }
  `,
})
export class ProviderStoreComponent implements OnInit {
  private readonly i18n = inject(I18nService);
  private readonly storeApi = inject(StoreService);
  private readonly toast = inject(ToastService);
  readonly store = signal<ProviderStoreDto | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly form = inject(FormBuilder).nonNullable.group({
    companyName: ['', Validators.required],
    phoneNumber: [''],
  });

  ngOnInit(): void {
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
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.busy.set(true);
    const value = this.form.getRawValue();
    this.storeApi.update(value.companyName, value.phoneNumber).subscribe({
      next: (s) => { this.store.set(s); this.toast.success(this.i18n.t('ui.saved')); this.busy.set(false); },
      error: () => this.busy.set(false),
    });
  }

  onFile(ev: Event): void {
    const file = (ev.target as HTMLInputElement).files?.[0];
    if (!file || this.busy()) return;
    this.busy.set(true);
    this.storeApi.uploadImage(file).pipe(finalize(() => this.busy.set(false))).subscribe({ next: () => { this.toast.success(this.i18n.t('ui.uploaded')); this.ngOnInit(); } });
  }
}
