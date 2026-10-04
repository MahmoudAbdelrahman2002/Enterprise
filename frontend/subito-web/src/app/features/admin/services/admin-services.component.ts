import { PageRequest } from '../../../core/utils/page-request';
import { FieldValidationDirective } from '../../../shared/directives/field-validation.directive';
import { fieldRules } from '../../../shared/forms/field-validators';
import { ActiveToggleComponent } from '../../../shared/components/active-toggle/active-toggle.component';
import { ImageUploadComponent } from '../../../shared/components/image-upload/image-upload.component';
import { I18nService } from '../../../core/services/i18n.service';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { finalize } from 'rxjs';
import { Component, Injector, OnInit, afterNextRender, inject, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { MarketplaceServiceDto } from '../../../core/models/domain.models';
import { CatalogService } from '../../../core/services/catalog.service';
import { ConfirmService } from '../../../core/services/confirm.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { readPage, resolvePage } from '../../../core/utils/read-list';
import { applyServerError, firstErrorKey } from '../../../shared/forms/form-errors';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { SearchFieldComponent } from '../../../shared/components/search-field/search-field.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-services',
  standalone: true,
  imports: [FieldValidationDirective, ActiveToggleComponent, ImageUploadComponent, IconComponent, ReactiveFormsModule, EmptyStateComponent, PaginationComponent, SearchFieldComponent, TranslatePipe],
  template: `
    <div class="toolbar">
      <h1 class="page-title" id="service-page-title" tabindex="-1">{{ 'nav.services' | t }}</h1>
      <div class="row">
        @if (!showForm()) { <app-search-field [control]="search" placeholderKey="search.services" /> }
        @if (canCreate && !showForm()) { <button class="btn btn-primary icon-action" type="button" id="create-service" (click)="startCreate()" [attr.aria-label]="'actions.create' | t" [title]="'actions.create' | t"><app-icon name="plus" />{{ 'actions.create' | t }}</button> }
      </div>
    </div>
    <p class="muted">{{ 'services.publicationHint' | t }}</p>
    @if (showForm()) {
      <form class="card stack" [formGroup]="form" (ngSubmit)="create()">
        <h2>{{ (editing() ? 'actions.edit' : 'actions.create') | t }}@if (editing(); as record) { : {{ record.name }} }</h2>
        <div class="field"><label for="admin-services-code">{{ 'ui.code' | t }}</label><input id="admin-services-code" appFieldValidation formControlName="code" /></div>
        <div class="field"><label for="admin-services-nameEn">{{ 'roles.nameEn' | t }}</label><input id="admin-services-nameEn" appFieldValidation formControlName="nameEn" /></div>
        <div class="field"><label for="admin-services-nameAr">{{ 'roles.nameAr' | t }}</label><input id="admin-services-nameAr" appFieldValidation formControlName="nameAr" dir="rtl" /></div>
        <div class="field"><label for="admin-services-nameIt">{{ 'roles.nameIt' | t }}</label><input id="admin-services-nameIt" appFieldValidation formControlName="nameIt" /></div>
        <div class="field"><label for="admin-services-displayOrder">{{ 'ui.displayOrder' | t }}</label><input id="admin-services-displayOrder" type="number" appFieldValidation formControlName="displayOrder" /></div>
        <div class="field"><label for="admin-services-descEn">{{ 'ui.descriptionEn' | t }}</label><textarea id="admin-services-descEn" appFieldValidation formControlName="descEn"></textarea></div>
        <div class="field"><label for="admin-services-descAr">{{ 'ui.descriptionAr' | t }}</label><textarea id="admin-services-descAr" appFieldValidation formControlName="descAr" dir="rtl"></textarea></div>
        <div class="field"><label for="admin-services-descIt">{{ 'ui.descriptionIt' | t }}</label><textarea id="admin-services-descIt" appFieldValidation formControlName="descIt"></textarea></div>
        <p class="muted">{{ 'catalogue.languageHint' | t }}</p>
        @if (form.errors?.['server']) { <p class="field-error">{{ form.errors?.['server'] }}</p> }
        <div class="row">
          <button class="btn btn-primary icon-action" type="submit" [disabled]="busy()" [attr.aria-label]="'actions.save' | t" [title]="'actions.save' | t"><app-icon name="check" />{{ 'actions.save' | t }}</button>
          <button class="btn btn-ghost icon-action" type="button" [disabled]="busy()" (click)="cancelEdit()" [attr.aria-label]="'actions.cancel' | t" [title]="'actions.cancel' | t"><app-icon name="close" />{{ 'actions.cancel' | t }}</button>
        </div>
      </form>
    }
    @if (!showForm()) {
    @if (loading()) { <p class="muted">{{ 'loading' | t }}</p> }
    @else if (failed()) { <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost icon-action" type="button" (click)="load(1)" [attr.aria-label]="'actions.retry' | t" [title]="'actions.retry' | t"><app-icon name="retry" /></button></div> } @else if (!items().length) { <app-empty-state /> } @else {
      <div class="table-wrap card"><table class="data">
        <thead><tr><th>{{ 'ui.code' | t }}</th><th>{{ 'ui.name' | t }}</th><th>{{ 'ui.displayOrder' | t }}</th><th>{{ 'ui.active' | t }}</th><th></th></tr></thead>
        <tbody>
          @for (s of items(); track s.id) {
            <tr>
              <td>{{ s.code }}</td><td>{{ s.name }}</td><td>{{ s.displayOrder }}</td><td>@if (canUpdate) { <app-active-toggle [targetName]="s.name" confirmationKey="confirm.deactivateService" [active]="s.isActive" [disabled]="busy()" (changed)="toggle(s)" /> } @else { <span class="badge">{{ (s.isActive ? 'status.active' : 'status.inactive') | t }}</span> }</td>
              <td class="row">
                @if (canUpdate) {
                  <button class="btn btn-ghost icon-action" type="button" [id]="'edit-service-' + s.id" [disabled]="busy()" (click)="edit(s)" [attr.aria-label]="('actions.edit' | t) + ': ' + s.name"><app-icon name="edit" />{{ 'actions.edit' | t }}</button>

                  <app-image-upload [disabled]="busy()" (selected)="upload(s.id, $event)" />
                }
                @if (canDelete) { <button class="btn btn-danger icon-action" type="button" [disabled]="busy()" (click)="remove(s.id)" [attr.aria-label]="'actions.delete' | t" [title]="'actions.delete' | t"><app-icon name="trash" /></button> }
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
export class AdminServicesComponent implements OnInit {
  private readonly pageRequest = new PageRequest(inject(DestroyRef));
  private readonly i18n = inject(I18nService);
  private readonly catalog = inject(CatalogService);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  private readonly confirm = inject(ConfirmService);
  readonly items = signal<MarketplaceServiceDto[]>([]);
  readonly editing = signal<MarketplaceServiceDto | null>(null);
  private readonly injector = inject(Injector);
  readonly showForm = signal(false);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly busy = signal(false);
  readonly search = new FormControl('', { nonNullable: true, validators: fieldRules.search });
  page = 1; totalPages = 1; totalCount = 0;
  readonly form = inject(FormBuilder).nonNullable.group({
    code: ['', fieldRules.code],
    nameEn: ['', fieldRules.title],
    descEn: ['', fieldRules.description],
    descAr: ['', fieldRules.description],
    descIt: ['', fieldRules.description],

    nameAr: ['', fieldRules.optionalTitle],
    nameIt: ['', fieldRules.optionalTitle],
    displayOrder: [0, fieldRules.displayOrder],
  });
  canCreate = this.tokens.hasPermission('admin', 'Services.Create');
  canUpdate = this.tokens.hasPermission('admin', 'Services.Update');
  canDelete = this.tokens.hasPermission('admin', 'Services.Delete');

  constructor() {
    this.search.valueChanges.pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed()).subscribe(() => { if (this.search.valid) this.load(1); });
  }

  ngOnInit(): void { this.load(1); }

  error(name: 'code' | 'nameEn' | 'displayOrder'): string | null {
    return firstErrorKey(this.form.controls[name]);
  }

  load(page: number, focusId?: string): void {
    this.page = page;
    this.loading.set(true); this.failed.set(false);
    this.pageRequest.run(this.catalog.listAdminServices({ pageNumber: page, pageSize: 20, searchTerm: this.search.value || null }), {
      next: (r) => {
        const pageData = readPage<MarketplaceServiceDto>(r);
        const targetPage = resolvePage(page, pageData);
        if (page !== targetPage) { this.load(targetPage, focusId); return; }
        this.items.set(pageData.items);
        this.totalPages = pageData.totalPages;
        this.totalCount = pageData.totalCount;
        this.loading.set(false);
        if (focusId) this.focus(focusId);
      },
      error: () => { this.failed.set(true); this.loading.set(false); },
    });
  }


  startCreate(): void {
    if (!this.canCreate || this.busy()) return;
    this.editing.set(null); this.form.reset({ displayOrder: 0 }); this.showForm.set(true);
    this.focus('admin-services-code');
  }

  edit(item: MarketplaceServiceDto): void {
    if (!this.canUpdate || this.busy()) return;
    this.busy.set(true);
    this.catalog.getAdminService(item.id).subscribe({
      next: (detail) => {
        this.editing.set(detail);
        this.form.reset({
          code: detail.code, displayOrder: detail.displayOrder,
          nameEn: detail.translations?.name.en ?? detail.name,
          nameAr: detail.translations?.name.ar ?? '', nameIt: detail.translations?.name.it ?? '',
          descEn: detail.translations?.description?.en ?? detail.description ?? '',
          descAr: detail.translations?.description?.ar ?? '', descIt: detail.translations?.description?.it ?? '',
        });
        this.showForm.set(true); this.busy.set(false); this.focus('admin-services-code');
      },
      error: () => this.busy.set(false),
    });
  }

  cancelEdit(): void {
    if (this.busy()) return;
    const record = this.editing();
    this.showForm.set(false); this.editing.set(null); this.form.reset({ displayOrder: 0 });
    this.focus(record ? 'edit-service-' + record.id : 'create-service');
  }

  private focus(id: string): void {
    afterNextRender(() => (document.getElementById(id) ?? document.getElementById('service-page-title'))?.focus(), { injector: this.injector });
  }

  create(): void {
    const editing = this.editing();
    if (this.busy() || (editing ? !this.canUpdate : !this.canCreate)) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.busy.set(true);
    const value = this.form.getRawValue();
    const body = {
      code: value.code, displayOrder: Number(value.displayOrder),
      name: { en: value.nameEn, ar: value.nameAr || null, it: value.nameIt || null },
      description: { en: value.descEn || null, ar: value.descAr || null, it: value.descIt || null },
    };
    const request = editing ? this.catalog.updateAdminService(editing.id, body) : this.catalog.createAdminService({ ...body, isActive: true });
    request.subscribe({
      next: () => {
        this.toast.success(this.i18n.t(editing ? 'ui.updated' : 'ui.created'));
        this.busy.set(false); this.cancelEdit();
        this.load(editing ? this.page : 1, editing ? 'edit-service-' + editing.id : 'create-service');
      },
      error: (err) => { applyServerError(this.form, err); this.busy.set(false); },
    });
  }

  toggle(s: MarketplaceServiceDto): void {
    if (this.busy()) return;
    this.busy.set(true);
    this.catalog.setAdminServiceActive(s.id, !s.isActive).subscribe({
      next: () => { this.load(this.page); this.busy.set(false); },
      error: () => this.busy.set(false),
    });
  }

  async remove(id: string): Promise<void> {
    if (this.busy()) return;
    if (!(await this.confirm.ask('confirm.deleteService', this.items().find(item => item.id === id)?.name))) return;
    this.busy.set(true);
    this.catalog.deleteAdminService(id).pipe(finalize(() => this.busy.set(false))).subscribe({ next: () => this.load(this.page) });
  }

  upload(id: string, file: File): void {
    if (!file || this.busy()) return;
    this.busy.set(true);
    this.catalog.uploadAdminServiceImage(id, file).pipe(finalize(() => this.busy.set(false))).subscribe({ next: () => this.toast.success(this.i18n.t('ui.uploaded')) });
  }
}
