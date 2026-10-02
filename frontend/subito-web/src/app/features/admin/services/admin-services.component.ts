import { ImageUploadComponent } from '../../../shared/components/image-upload/image-upload.component';
import { I18nService } from '../../../core/services/i18n.service';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { finalize } from 'rxjs';
import { Component, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { MarketplaceServiceDto } from '../../../core/models/domain.models';
import { CatalogService } from '../../../core/services/catalog.service';
import { ConfirmService } from '../../../core/services/confirm.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { readPage } from '../../../core/utils/read-list';
import { applyServerError, firstErrorKey } from '../../../shared/forms/form-errors';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { SearchFieldComponent } from '../../../shared/components/search-field/search-field.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-services',
  standalone: true,
  imports: [ImageUploadComponent, IconComponent, ReactiveFormsModule, EmptyStateComponent, PaginationComponent, SearchFieldComponent, TranslatePipe],
  template: `
    <div class="toolbar">
      <h1 class="page-title">{{ 'nav.services' | t }}</h1>
      <div class="row">
        <app-search-field [control]="search" placeholderKey="search.services" />
        @if (canCreate) { <button class="btn btn-primary" type="button" (click)="showForm.set(true)">{{ 'actions.create' | t }}</button> }
      </div>
    </div>
    @if (showForm()) {
      <form class="card stack" [formGroup]="form" (ngSubmit)="create()">
        <div class="field"><label for="admin-services-code">{{ 'ui.code' | t }}</label><input id="admin-services-code" formControlName="code" />
          @if (error('code'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="admin-services-nameEn">{{ 'roles.nameEn' | t }}</label><input id="admin-services-nameEn" formControlName="nameEn" />
          @if (error('nameEn'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="admin-services-nameAr">{{ 'roles.nameAr' | t }}</label><input id="admin-services-nameAr" formControlName="nameAr" dir="rtl" /></div>
        <div class="field"><label for="admin-services-nameIt">{{ 'roles.nameIt' | t }}</label><input id="admin-services-nameIt" formControlName="nameIt" /></div>
        <div class="field"><label for="admin-services-displayOrder">{{ 'ui.displayOrder' | t }}</label><input id="admin-services-displayOrder" type="number" formControlName="displayOrder" />
          @if (error('displayOrder'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
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
        <thead><tr><th>{{ 'ui.code' | t }}</th><th>{{ 'ui.name' | t }}</th><th>{{ 'ui.displayOrder' | t }}</th><th>{{ 'ui.active' | t }}</th><th></th></tr></thead>
        <tbody>
          @for (s of items(); track s.id) {
            <tr>
              <td>{{ s.code }}</td><td>{{ s.name }}</td><td>{{ s.displayOrder }}</td><td><span class="badge">{{ (s.isActive ? 'status.active' : 'status.inactive') | t }}</span></td>
              <td class="row">
                @if (canUpdate) {
                  <button class="btn btn-ghost" type="button" (click)="toggle(s)">{{ s.isActive ? ('actions.deactivate'|t) : ('actions.activate'|t) }}</button>
                  <app-image-upload [disabled]="busy()" (selected)="upload(s.id, $event)" />
                }
                @if (canDelete) { <button class="btn btn-danger" type="button" [disabled]="busy()" (click)="remove(s.id)"><app-icon name="trash" />{{ 'actions.delete' | t }}</button> }
              </td>
            </tr>
          }
        </tbody>
      </table></div>
      <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" (change)="load($event)" />
    }
  `,
})
export class AdminServicesComponent implements OnInit {
  private readonly i18n = inject(I18nService);
  private readonly catalog = inject(CatalogService);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  private readonly confirm = inject(ConfirmService);
  readonly items = signal<MarketplaceServiceDto[]>([]);
  readonly showForm = signal(false);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly busy = signal(false);
  readonly search = new FormControl('', { nonNullable: true });
  page = 1; totalPages = 1; totalCount = 0;
  readonly form = inject(FormBuilder).nonNullable.group({
    code: ['', Validators.required],
    nameEn: ['', Validators.required],
    nameAr: [''],
    nameIt: [''],
    displayOrder: [0, [Validators.required, Validators.min(0)]],
  });
  canCreate = this.tokens.hasPermission('admin', 'Services.Create');
  canUpdate = this.tokens.hasPermission('admin', 'Services.Update');
  canDelete = this.tokens.hasPermission('admin', 'Services.Delete');

  constructor() {
    this.search.valueChanges.pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed()).subscribe(() => this.load(1));
  }

  ngOnInit(): void { this.load(1); }

  error(name: 'code' | 'nameEn' | 'displayOrder'): string | null {
    return firstErrorKey(this.form.controls[name]);
  }

  load(page: number): void {
    this.page = page;
    this.loading.set(true); this.failed.set(false);
    this.catalog.listAdminServices({ pageNumber: page, pageSize: 20, searchTerm: this.search.value || null }).subscribe({
      next: (r) => {
        const pageData = readPage<MarketplaceServiceDto>(r);
        this.items.set(pageData.items);
        this.totalPages = pageData.totalPages;
        this.totalCount = pageData.totalCount;
        this.loading.set(false);
      },
      error: () => { this.failed.set(true); this.loading.set(false); },
    });
  }

  create(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.busy.set(true);
    const value = this.form.getRawValue();
    this.catalog.createAdminService({
      code: value.code,
      displayOrder: Number(value.displayOrder),
      isActive: true,
      name: { en: value.nameEn, ar: value.nameAr || null, it: value.nameIt || null },
    }).subscribe({
      next: () => { this.toast.success(this.i18n.t('ui.created')); this.showForm.set(false); this.form.reset({ displayOrder: 0 }); this.busy.set(false); this.load(1); },
      error: (err) => { applyServerError(this.form, err); this.busy.set(false); },
    });
  }

  toggle(s: MarketplaceServiceDto): void {
    this.catalog.setAdminServiceActive(s.id, !s.isActive).subscribe({ next: () => this.load(this.page) });
  }

  async remove(id: string): Promise<void> {
    if (this.busy()) return;
    if (!(await this.confirm.ask())) return;
    this.busy.set(true);
    this.catalog.deleteAdminService(id).pipe(finalize(() => this.busy.set(false))).subscribe({ next: () => this.load(this.page) });
  }

  upload(id: string, ev: Event): void {
    const file = (ev.target as HTMLInputElement).files?.[0];
    if (!file || this.busy()) return;
    this.busy.set(true);
    this.catalog.uploadAdminServiceImage(id, file).pipe(finalize(() => this.busy.set(false))).subscribe({ next: () => this.toast.success(this.i18n.t('ui.uploaded')) });
  }
}
