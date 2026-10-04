import { PageRequest } from '../../../core/utils/page-request';
import { FieldValidationDirective } from '../../../shared/directives/field-validation.directive';
import { fieldRules } from '../../../shared/forms/field-validators';
import { ActiveToggleComponent } from '../../../shared/components/active-toggle/active-toggle.component';
import { ImageUploadComponent } from '../../../shared/components/image-upload/image-upload.component';
import { I18nService } from '../../../core/services/i18n.service';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { finalize } from 'rxjs';
import { Component, Injector, OnInit, afterNextRender, inject, signal, DestroyRef } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CategoryDetailDto } from '../../../core/models/domain.models';
import { CatalogService } from '../../../core/services/catalog.service';
import { ConfirmService } from '../../../core/services/confirm.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { readPage, resolvePage } from '../../../core/utils/read-list';
import { applyServerError, firstErrorKey } from '../../../shared/forms/form-errors';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-provider-categories',
  standalone: true,
  imports: [RouterLink, FieldValidationDirective, ActiveToggleComponent, ImageUploadComponent, IconComponent, ReactiveFormsModule, EmptyStateComponent, PaginationComponent, TranslatePipe],
  template: `
    <div class="toolbar">
      <h1 class="page-title" id="category-page-title" tabindex="-1">{{ 'nav.categories' | t }}</h1>
      @if (canCreate && !showForm()) { <button class="btn btn-primary icon-action" type="button" id="create-category" (click)="startCreate()" [attr.aria-label]="'actions.create' | t" [title]="'actions.create' | t"><app-icon name="plus" />{{ 'actions.create' | t }}</button> }
    </div>
    @if (showForm()) {
      <form class="card stack" [formGroup]="form" (ngSubmit)="create()">
        <h2>{{ (editing() ? 'actions.edit' : 'actions.create') | t }}@if (editing(); as record) { : {{ record.name }} }</h2>
        <div class="field"><label for="provider-categories-nameEn">{{ 'roles.nameEn' | t }}</label><input id="provider-categories-nameEn" appFieldValidation formControlName="nameEn" /></div>
        <div class="field"><label for="provider-categories-descEn">{{ 'ui.descriptionEn' | t }}</label><textarea id="provider-categories-descEn" appFieldValidation formControlName="descEn"></textarea></div>
        <div class="field"><label for="provider-categories-displayOrder">{{ 'ui.displayOrder' | t }}</label><input id="provider-categories-displayOrder" type="number" appFieldValidation formControlName="displayOrder" /></div>
        <div class="field"><label for="provider-categories-nameAr">{{ 'roles.nameAr' | t }}</label><input id="provider-categories-nameAr" appFieldValidation formControlName="nameAr" dir="rtl" /></div>
        <div class="field"><label for="provider-categories-nameIt">{{ 'roles.nameIt' | t }}</label><input id="provider-categories-nameIt" appFieldValidation formControlName="nameIt" /></div>
        <div class="field"><label for="provider-categories-descAr">{{ 'ui.descriptionAr' | t }}</label><textarea id="provider-categories-descAr" appFieldValidation formControlName="descAr" dir="rtl"></textarea></div>
        <div class="field"><label for="provider-categories-descIt">{{ 'ui.descriptionIt' | t }}</label><textarea id="provider-categories-descIt" appFieldValidation formControlName="descIt"></textarea></div>
        <p class="muted">{{ 'catalogue.languageHint' | t }}</p>
        @if (form.errors?.['server']) { <p class="field-error">{{ form.errors?.['server'] }}</p> }
        <div class="row">
          <button class="btn btn-primary icon-action" type="submit" [disabled]="busy()" [attr.aria-label]="'actions.save' | t" [title]="'actions.save' | t"><app-icon name="check" />{{ 'actions.save' | t }}</button>
          <button class="btn btn-ghost icon-action" type="button" [disabled]="busy()" (click)="cancelEdit()" [attr.aria-label]="'actions.cancel' | t" [title]="'actions.cancel' | t"><app-icon name="close" />{{ 'actions.cancel' | t }}</button>
        </div>
      </form>
    }
    @if (!showForm()) {
    @if (deleteError(); as messageKey) {
      <div class="card stack" role="alert"><strong>{{ deleteTarget() }}</strong><p>{{ messageKey | t }}</p>
        @if (deleteError() === 'category.hasProducts' && canReadProducts) { <a class="btn btn-ghost" routerLink="/provider/products">{{ 'nav.products' | t }}</a> }
      </div>
    }
    @if (loading()) { <p class="muted">{{ 'loading' | t }}</p> }
    @else if (failed()) { <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost icon-action" type="button" (click)="load(1)" [attr.aria-label]="'actions.retry' | t" [title]="'actions.retry' | t"><app-icon name="retry" /></button></div> } @else if (!items().length) {
      <app-empty-state [messageKey]="canCreate ? 'empty.categoriesSetup' : 'empty.categories'" icon="grid">
        @if (canCreate) { <button class="btn btn-primary" type="button" (click)="startCreate()">{{ 'empty.createCategory' | t }}</button> }
      </app-empty-state>
    } @else {
      <div class="table-wrap card"><table class="data">
        <thead><tr><th>{{ 'ui.name' | t }}</th><th>{{ 'ui.displayOrder' | t }}</th><th>{{ 'ui.active' | t }}</th><th></th></tr></thead>
        <tbody>
          @for (c of items(); track c.id) {
            <tr>
              <td><div class="provider-identity">@if (c.imageUrl) { <a [href]="c.imageUrl" target="_blank" rel="noopener" [attr.aria-label]="('ui.previewImage' | t) + ': ' + c.name"><img class="thumb" [src]="c.imageUrl" [alt]="c.name" loading="lazy" /></a> }<span>{{ c.name }}</span></div></td><td>{{ c.displayOrder }}</td>
              <td>@if (canUpdate) { <app-active-toggle [targetName]="c.name" confirmationKey="confirm.deactivateCategory" [active]="c.isActive" [disabled]="busy()" (changed)="toggle(c)" /> } @else { <span class="badge" [class.badge-success]="c.isActive">{{ (c.isActive ? 'status.active' : 'status.inactive') | t }}</span> }</td>
              <td class="row">
                @if (canUpdate) {
                  <button class="btn btn-ghost icon-action" type="button" [id]="'edit-category-' + c.id" [disabled]="busy()" (click)="edit(c)" [attr.aria-label]="('actions.edit' | t) + ': ' + c.name"><app-icon name="edit" />{{ 'actions.edit' | t }}</button>

                  <app-image-upload [disabled]="busy()" (selected)="upload(c.id, $event)" />
                }
                @if (canDelete) { <button class="btn btn-danger icon-action" type="button" [disabled]="busy()" (click)="remove(c.id)" [attr.aria-label]="'actions.delete' | t" [title]="'actions.delete' | t"><app-icon name="trash" /></button> }
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
export class ProviderCategoriesComponent implements OnInit {
  private readonly pageRequest = new PageRequest(inject(DestroyRef));
  private readonly i18n = inject(I18nService);
  private readonly catalog = inject(CatalogService);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  private readonly confirm = inject(ConfirmService);
  readonly items = signal<CategoryDetailDto[]>([]);
  readonly editing = signal<CategoryDetailDto | null>(null);
  private readonly injector = inject(Injector);
  readonly showForm = signal(false);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly busy = signal(false);
  readonly deleteError = signal<string | null>(null);
  readonly deleteTarget = signal('');
  readonly canReadProducts = this.tokens.hasPermission('provider', 'ProviderProduct.Read');
  page = 1; totalPages = 1; totalCount = 0;
  readonly form = inject(FormBuilder).nonNullable.group({
    nameEn: ['', fieldRules.title],
    nameAr: ['', fieldRules.optionalTitle],
    nameIt: ['', fieldRules.optionalTitle],
    descAr: ['', fieldRules.description],
    descIt: ['', fieldRules.description],

    descEn: ['', fieldRules.description],
    displayOrder: [0, fieldRules.displayOrder],
  });
  canCreate = this.tokens.hasPermission('provider', 'ProviderCategory.Create');
  canUpdate = this.tokens.hasPermission('provider', 'ProviderCategory.Update');
  canDelete = this.tokens.hasPermission('provider', 'ProviderCategory.Delete');

  ngOnInit(): void { this.load(1); }

  error(name: 'nameEn' | 'displayOrder'): string | null {
    return firstErrorKey(this.form.controls[name]);
  }

  load(page: number, focusId?: string): void {
    this.page = page;
    this.loading.set(true); this.failed.set(false);
    this.pageRequest.run(this.catalog.listProviderCategories({ pageNumber: page, pageSize: 20 }), {
      next: (r) => {
        const pageData = readPage<CategoryDetailDto>(r);
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
    this.focus('provider-categories-nameEn');
  }

  edit(item: CategoryDetailDto): void {
    if (!this.canUpdate || this.busy()) return;
    this.busy.set(true);
    this.catalog.getProviderCategory(item.id).subscribe({
      next: (detail) => {
        this.editing.set(detail);
        this.form.reset({
          displayOrder: detail.displayOrder,
          nameEn: detail.translations?.name.en ?? detail.name,
          nameAr: detail.translations?.name.ar ?? '', nameIt: detail.translations?.name.it ?? '',
          descEn: detail.translations?.description?.en ?? detail.description ?? '',
          descAr: detail.translations?.description?.ar ?? '', descIt: detail.translations?.description?.it ?? '',
        });
        this.showForm.set(true); this.busy.set(false); this.focus('provider-categories-nameEn');
      },
      error: () => this.busy.set(false),
    });
  }

  cancelEdit(): void {
    if (this.busy()) return;
    const record = this.editing();
    this.showForm.set(false); this.editing.set(null); this.form.reset({ displayOrder: 0 });
    this.focus(record ? 'edit-category-' + record.id : 'create-category');
  }

  private focus(id: string): void {
    afterNextRender(() => (document.getElementById(id) ?? document.getElementById('category-page-title'))?.focus(), { injector: this.injector });
  }

  create(): void {
    const editing = this.editing();
    if (this.busy() || (editing ? !this.canUpdate : !this.canCreate)) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.busy.set(true);
    const value = this.form.getRawValue();
    const body = {
      displayOrder: Number(value.displayOrder),
      name: { en: value.nameEn, ar: value.nameAr || null, it: value.nameIt || null },
      description: { en: value.descEn || null, ar: value.descAr || null, it: value.descIt || null },
    };
    const request = editing ? this.catalog.updateProviderCategory(editing.id, body) : this.catalog.createProviderCategory({ ...body, isActive: true });
    request.subscribe({
      next: () => {
        this.toast.success(this.i18n.t(editing ? 'ui.updated' : 'ui.created'));
        this.busy.set(false); this.cancelEdit();
        this.load(editing ? this.page : 1, editing ? 'edit-category-' + editing.id : 'create-category');
      },
      error: (err) => { applyServerError(this.form, err); this.busy.set(false); },
    });
  }

  toggle(c: CategoryDetailDto): void {
    if (this.busy()) return;
    this.busy.set(true);
    this.catalog.setProviderCategoryActive(c.id, !c.isActive).subscribe({
      next: () => { this.load(this.page); this.busy.set(false); },
      error: () => this.busy.set(false),
    });
  }

  async remove(id: string): Promise<void> {
    if (this.busy()) return;
    const category = this.items().find(item => item.id === id);
    if (!(await this.confirm.ask('confirm.deleteCategory', category?.name))) return;
    this.deleteError.set(null); this.deleteTarget.set(category?.name ?? '');
    this.busy.set(true);
    this.catalog.deleteProviderCategory(id).pipe(finalize(() => this.busy.set(false))).subscribe({
      next: () => this.load(this.page),
      error: (error) => this.deleteError.set(error?.statusCode === 409 ? 'category.hasProducts' : 'errors.generic'),
    });
  }

  upload(id: string, file: File): void {
    if (!file || this.busy()) return;
    this.busy.set(true);
    this.catalog.uploadProviderCategoryImage(id, file).pipe(finalize(() => this.busy.set(false))).subscribe({ next: () => { this.toast.success(this.i18n.t('ui.uploaded')); this.load(this.page); } });
  }
}
