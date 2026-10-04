import { readAllPages } from '../../../core/utils/read-all-pages';
import { PageRequest } from '../../../core/utils/page-request';
import { FieldValidationDirective } from '../../../shared/directives/field-validation.directive';
import { fieldRules } from '../../../shared/forms/field-validators';
import { ImageUploadComponent } from '../../../shared/components/image-upload/image-upload.component';
import { I18nService } from '../../../core/services/i18n.service';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { finalize } from 'rxjs';
import { Component, Injector, OnInit, afterNextRender, inject, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { CategoryDetailDto, ProductDetailDto, ProductListItemDto } from '../../../core/models/domain.models';
import { CatalogService } from '../../../core/services/catalog.service';
import { ConfirmService } from '../../../core/services/confirm.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { readList, readPage, resolvePage } from '../../../core/utils/read-list';
import { applyServerError, firstErrorKey } from '../../../shared/forms/form-errors';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { SearchFieldComponent } from '../../../shared/components/search-field/search-field.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-provider-products',
  standalone: true,
  imports: [RouterLink, FieldValidationDirective, ImageUploadComponent, IconComponent, MoneyPipe, ReactiveFormsModule, FormsModule, EmptyStateComponent, PaginationComponent, SearchFieldComponent, TranslatePipe],
  template: `
    <div class="toolbar">
      <h1 class="page-title" id="product-page-title" tabindex="-1">{{ 'nav.products' | t }}</h1>
      <div class="row">
        @if (!showForm()) { <app-search-field [control]="search" placeholderKey="search.products" /> }
        @if (canCreate && !showForm()) { <button class="btn btn-primary icon-action" type="button" id="create-product" (click)="startCreate()" [attr.aria-label]="'actions.create' | t" [title]="'actions.create' | t"><app-icon name="plus" />{{ 'actions.create' | t }}</button> }
      </div>
    </div>
    @if (showForm()) {
      <form class="card stack" [formGroup]="form" (ngSubmit)="create()">
        <h2>{{ (editing() ? 'actions.edit' : 'actions.create') | t }}@if (editing(); as record) { : {{ record.name }} }</h2>
        <div class="field"><label for="provider-products-categoryId">{{ 'ui.category' | t }}</label><select id="provider-products-categoryId" appFieldValidation formControlName="categoryId">
            <option value="">—</option>
            @if (currentCategoryMissing) { <option [value]="editing()!.categoryId">{{ 'ui.currentCategory' | t }}</option> }
            @for (c of categories(); track c.id) { <option [value]="c.id">{{ c.name }}</option> }
          </select>
        </div>
        <div class="field"><label for="provider-products-nameEn">{{ 'roles.nameEn' | t }}</label><input id="provider-products-nameEn" appFieldValidation formControlName="nameEn" /></div>
        <div class="field"><label for="provider-products-sku">{{ 'ui.sku' | t }}</label><input id="provider-products-sku" appFieldValidation formControlName="sku" /></div>
        <div class="field"><label for="provider-products-price">{{ 'ui.price' | t }}</label><input id="provider-products-price" type="number" step="0.01" appFieldValidation formControlName="price" /></div>
        <div class="field"><label for="provider-products-status">{{ 'ui.status' | t }}</label><select id="provider-products-status" appFieldValidation formControlName="status">
            <option [ngValue]="0">{{ 'status.draft' | t }}</option><option [ngValue]="1">{{ 'status.active' | t }}</option><option [ngValue]="2">{{ 'status.inactive' | t }}</option>
          </select>
        </div>
        <div class="field"><label for="provider-products-nameAr">{{ 'roles.nameAr' | t }}</label><input id="provider-products-nameAr" appFieldValidation formControlName="nameAr" dir="rtl" /></div>
        <div class="field"><label for="provider-products-nameIt">{{ 'roles.nameIt' | t }}</label><input id="provider-products-nameIt" appFieldValidation formControlName="nameIt" /></div>
        <div class="field"><label for="provider-products-descEn">{{ 'ui.descriptionEn' | t }}</label><textarea id="provider-products-descEn" appFieldValidation formControlName="descEn"></textarea></div>
        <div class="field"><label for="provider-products-descAr">{{ 'ui.descriptionAr' | t }}</label><textarea id="provider-products-descAr" appFieldValidation formControlName="descAr" dir="rtl"></textarea></div>
        <div class="field"><label for="provider-products-descIt">{{ 'ui.descriptionIt' | t }}</label><textarea id="provider-products-descIt" appFieldValidation formControlName="descIt"></textarea></div>
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
    @else if (failed()) { <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost icon-action" type="button" (click)="load(1)" [attr.aria-label]="'actions.retry' | t" [title]="'actions.retry' | t"><app-icon name="retry" /></button></div> } @else if (!items().length) {
      <app-empty-state [messageKey]="emptyMessageKey" [icon]="search.value.trim() ? 'search' : 'package'">
        @if (search.value.trim()) {
          <button class="btn btn-ghost" type="button" (click)="search.setValue('')">{{ 'actions.clear' | t }}</button>
        } @else if (canCreate) {
          @if (categories().length) {
            <button class="btn btn-primary" type="button" (click)="startCreate()">{{ 'empty.createProduct' | t }}</button>
          } @else if (canSetupCategories) {
            <a class="btn btn-primary" routerLink="/provider/categories">{{ 'empty.setupCategories' | t }}</a>
          }
        }
      </app-empty-state>
    } @else {
      <div class="table-wrap card"><table class="data">
        <thead><tr><th>{{ 'ui.name' | t }}</th><th>{{ 'ui.sku' | t }}</th><th>{{ 'ui.price' | t }}</th><th>{{ 'ui.status' | t }}</th><th></th></tr></thead>
        <tbody>
          @for (p of items(); track p.id) {
            <tr>
              <td><div class="provider-identity">@if (p.imageUrl) { <a [href]="p.imageUrl" target="_blank" rel="noopener" [attr.aria-label]="('ui.previewImage' | t) + ': ' + p.name"><img class="thumb" [src]="p.imageUrl" [alt]="p.name" loading="lazy" /></a> }<span>{{ p.name }}</span></div></td><td>{{ p.sku }}</td><td>{{ p.price | money }}</td><td><span class="badge" [class.badge-success]="p.status === 1">{{ (p.status === 0 ? 'status.draft' : p.status === 1 ? 'status.active' : 'status.inactive') | t }}</span></td>
              <td class="row">
                @if (canUpdate) { <button class="btn btn-ghost icon-action" type="button" [id]="'edit-product-' + p.id" [disabled]="busy()" (click)="edit(p)" [attr.aria-label]="('actions.edit' | t) + ': ' + p.name"><app-icon name="edit" />{{ 'actions.edit' | t }}</button><app-image-upload [disabled]="busy()" (selected)="upload(p, $event)" /> }
                @if (canDelete) { <button class="btn btn-danger icon-action" type="button" [disabled]="busy()" (click)="remove(p)" [attr.aria-label]="'actions.delete' | t" [title]="'actions.delete' | t"><app-icon name="trash" /></button> }
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
export class ProviderProductsComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly pageRequest = new PageRequest(this.destroyRef);
  private readonly i18n = inject(I18nService);
  private readonly catalog = inject(CatalogService);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  private readonly confirm = inject(ConfirmService);
  readonly items = signal<ProductListItemDto[]>([]);
  readonly categories = signal<CategoryDetailDto[]>([]);
  readonly editing = signal<ProductDetailDto | null>(null);
  private readonly injector = inject(Injector);
  readonly showForm = signal(false);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly busy = signal(false);
  readonly search = new FormControl('', { nonNullable: true, validators: fieldRules.search });
  page = 1; totalPages = 1; totalCount = 0;
  readonly form = inject(FormBuilder).nonNullable.group({
    categoryId: ['', fieldRules.id],
    nameEn: ['', fieldRules.title],
    nameAr: ['', fieldRules.optionalTitle],
    nameIt: ['', fieldRules.optionalTitle],
    descEn: ['', fieldRules.description],
    descAr: ['', fieldRules.description],
    descIt: ['', fieldRules.description],

    sku: ['', fieldRules.sku],
    price: [0, fieldRules.price],
    status: [1, fieldRules.productStatus],
  });
  canCreate = this.tokens.hasPermission('provider', 'ProviderProduct.Create');
  readonly canSetupCategories = this.tokens.hasPermission('provider', 'ProviderCategory.Read')
    && this.tokens.hasPermission('provider', 'ProviderCategory.Create');

  get emptyMessageKey(): string {
    if (this.search.value.trim()) return 'empty.productsSearch';
    if (!this.canCreate) return 'empty.products';
    if (!this.categories().length) return 'empty.productsNeedCategory';
    return 'empty.productsSetup';
  }
  canUpdate = this.tokens.hasPermission('provider', 'ProviderProduct.Update');
  canDelete = this.tokens.hasPermission('provider', 'ProviderProduct.Delete');

  get currentCategoryMissing(): boolean {
    const record = this.editing();
    return !!record && !this.categories().some(category => category.id === record.categoryId);
  }

  constructor() {
    this.search.valueChanges.pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed()).subscribe(() => { if (this.search.valid) this.load(1); });
  }

  ngOnInit(): void {
    if (this.tokens.hasPermission('provider', 'ProviderCategory.Read')) this.catalog.lookupProviderCategories().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => this.categories.set(readList<CategoryDetailDto>(res)),
      error: () => {
        readAllPages<CategoryDetailDto>(page => this.catalog.listProviderCategories({ pageNumber: page, pageSize: 100 })).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: (r) => this.categories.set(readList<CategoryDetailDto>(r)),
        });
      },
    });
    this.load(1);
  }

  error(name: 'categoryId' | 'nameEn' | 'sku' | 'price'): string | null {
    return firstErrorKey(this.form.controls[name]);
  }

  load(page: number, focusId?: string): void {
    this.page = page;
    this.loading.set(true); this.failed.set(false);
    this.pageRequest.run(this.catalog.listProviderProducts({ pageNumber: page, pageSize: 20, searchTerm: this.search.value || null }), {
      next: (r) => {
        const pageData = readPage<ProductListItemDto>(r);
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
    this.editing.set(null); this.form.reset({ price: 0, status: 1 }); this.showForm.set(true);
    this.focus('provider-products-categoryId');
  }

  edit(item: ProductListItemDto): void {
    if (!this.canUpdate || this.busy()) return;
    this.busy.set(true);
    this.catalog.getProviderProduct(item.categoryId, item.id).subscribe({
      next: (detail) => {
        this.editing.set(detail);
        this.form.reset({
          categoryId: detail.categoryId, sku: detail.sku, price: detail.price, status: detail.status,
          nameEn: detail.translations?.name.en ?? detail.name,
          nameAr: detail.translations?.name.ar ?? '', nameIt: detail.translations?.name.it ?? '',
          descEn: detail.translations?.description?.en ?? detail.description ?? '',
          descAr: detail.translations?.description?.ar ?? '', descIt: detail.translations?.description?.it ?? '',
        });
        this.showForm.set(true); this.busy.set(false); this.focus('provider-products-categoryId');
      },
      error: () => this.busy.set(false),
    });
  }

  cancelEdit(): void {
    if (this.busy()) return;
    const record = this.editing();
    this.showForm.set(false); this.editing.set(null); this.form.reset({ price: 0, status: 1 });
    this.focus(record ? 'edit-product-' + record.id : 'create-product');
  }

  private focus(id: string): void {
    afterNextRender(() => (document.getElementById(id) ?? document.getElementById('product-page-title'))?.focus(), { injector: this.injector });
  }

  create(): void {
    const editing = this.editing();
    if (this.busy() || (editing ? !this.canUpdate : !this.canCreate)) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.busy.set(true);
    const value = this.form.getRawValue();
    const body = {
      sku: value.sku, price: Number(value.price), status: value.status, categoryId: value.categoryId,
      name: { en: value.nameEn, ar: value.nameAr || null, it: value.nameIt || null },
      description: { en: value.descEn || null, ar: value.descAr || null, it: value.descIt || null },
    };
    const request = editing ? this.catalog.updateProviderProduct(editing.categoryId, editing.id, body) : this.catalog.createProviderProduct(value.categoryId, body);
    request.subscribe({
      next: () => {
        this.toast.success(this.i18n.t(editing ? 'ui.updated' : 'ui.created'));
        this.busy.set(false); this.cancelEdit();
        this.load(editing ? this.page : 1, editing ? 'edit-product-' + editing.id : 'create-product');
      },
      error: (err) => { applyServerError(this.form, err); this.busy.set(false); },
    });
  }

  async remove(p: ProductListItemDto): Promise<void> {
    if (this.busy()) return;
    if (!(await this.confirm.ask('confirm.deleteProduct', p.name))) return;
    this.busy.set(true);
    this.catalog.deleteProviderProduct(p.categoryId, p.id).pipe(finalize(() => this.busy.set(false))).subscribe({ next: () => this.load(this.page) });
  }

  upload(p: ProductListItemDto, file: File): void {
    if (!file || this.busy()) return;
    this.busy.set(true);
    this.catalog.uploadProviderProductImage(p.categoryId, p.id, file).pipe(finalize(() => this.busy.set(false))).subscribe({
      next: () => { this.toast.success(this.i18n.t('ui.uploaded')); this.load(this.page); },
    });
  }
}
