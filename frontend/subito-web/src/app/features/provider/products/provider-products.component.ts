import { ImageUploadComponent } from '../../../shared/components/image-upload/image-upload.component';
import { I18nService } from '../../../core/services/i18n.service';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { finalize } from 'rxjs';
import { Component, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { CategoryDetailDto, ProductListItemDto } from '../../../core/models/domain.models';
import { CatalogService } from '../../../core/services/catalog.service';
import { ConfirmService } from '../../../core/services/confirm.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { readList, readPage } from '../../../core/utils/read-list';
import { applyServerError, firstErrorKey } from '../../../shared/forms/form-errors';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { SearchFieldComponent } from '../../../shared/components/search-field/search-field.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-provider-products',
  standalone: true,
  imports: [ImageUploadComponent, IconComponent, MoneyPipe, ReactiveFormsModule, FormsModule, EmptyStateComponent, PaginationComponent, SearchFieldComponent, TranslatePipe],
  template: `
    <div class="toolbar">
      <h1 class="page-title">{{ 'nav.products' | t }}</h1>
      <div class="row">
        <app-search-field [control]="search" placeholderKey="search.products" />
        @if (canCreate) { <button class="btn btn-primary" type="button" (click)="showForm.set(true)">{{ 'actions.create' | t }}</button> }
      </div>
    </div>
    @if (showForm()) {
      <form class="card stack" [formGroup]="form" (ngSubmit)="create()">
        <div class="field"><label for="provider-products-categoryId">{{ 'ui.category' | t }}</label><select id="provider-products-categoryId" formControlName="categoryId">
            <option value="">—</option>
            @for (c of categories(); track c.id) { <option [value]="c.id">{{ c.name }}</option> }
          </select>
          @if (error('categoryId'); as key) { <small class="field-error">{{ key | t }}</small> }
        </div>
        <div class="field"><label for="provider-products-nameEn">{{ 'roles.nameEn' | t }}</label><input id="provider-products-nameEn" formControlName="nameEn" />
          @if (error('nameEn'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="provider-products-sku">{{ 'ui.sku' | t }}</label><input id="provider-products-sku" formControlName="sku" />
          @if (error('sku'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="provider-products-price">{{ 'ui.price' | t }}</label><input id="provider-products-price" type="number" step="0.01" formControlName="price" />
          @if (error('price'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="provider-products-status">{{ 'ui.status' | t }}</label><select id="provider-products-status" formControlName="status">
            <option [ngValue]="0">{{ 'status.draft' | t }}</option><option [ngValue]="1">{{ 'status.active' | t }}</option><option [ngValue]="2">{{ 'status.inactive' | t }}</option>
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
        <thead><tr><th>{{ 'ui.name' | t }}</th><th>{{ 'ui.sku' | t }}</th><th>{{ 'ui.price' | t }}</th><th>{{ 'ui.status' | t }}</th><th></th></tr></thead>
        <tbody>
          @for (p of items(); track p.id) {
            <tr>
              <td>{{ p.name }}</td><td>{{ p.sku }}</td><td>{{ p.price | money }}</td><td><span class="badge" [class.badge-success]="p.status === 1">{{ (p.status === 0 ? 'status.draft' : p.status === 1 ? 'status.active' : 'status.inactive') | t }}</span></td>
              <td class="row">
                @if (canUpdate) { <app-image-upload [disabled]="busy()" (selected)="upload(p, $event)" /> }
                @if (canDelete) { <button class="btn btn-danger" type="button" [disabled]="busy()" (click)="remove(p)"><app-icon name="trash" />{{ 'actions.delete' | t }}</button> }
              </td>
            </tr>
          }
        </tbody>
      </table></div>
      <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" (change)="load($event)" />
    }
  `,
})
export class ProviderProductsComponent implements OnInit {
  private readonly i18n = inject(I18nService);
  private readonly catalog = inject(CatalogService);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  private readonly confirm = inject(ConfirmService);
  readonly items = signal<ProductListItemDto[]>([]);
  readonly categories = signal<CategoryDetailDto[]>([]);
  readonly showForm = signal(false);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly busy = signal(false);
  readonly search = new FormControl('', { nonNullable: true });
  page = 1; totalPages = 1; totalCount = 0;
  readonly form = inject(FormBuilder).nonNullable.group({
    categoryId: ['', Validators.required],
    nameEn: ['', Validators.required],
    sku: ['', Validators.required],
    price: [0, [Validators.required, Validators.min(0)]],
    status: [1],
  });
  canCreate = this.tokens.hasPermission('provider', 'ProviderProduct.Create');
  canUpdate = this.tokens.hasPermission('provider', 'ProviderProduct.Update');
  canDelete = this.tokens.hasPermission('provider', 'ProviderProduct.Delete');

  constructor() {
    this.search.valueChanges.pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed()).subscribe(() => this.load(1));
  }

  ngOnInit(): void {
    this.catalog.lookupProviderCategories().subscribe({
      next: (res) => this.categories.set(readList<CategoryDetailDto>(res)),
      error: () => {
        this.catalog.listProviderCategories({ pageSize: 100 }).subscribe({
          next: (r) => this.categories.set(readList<CategoryDetailDto>(r)),
        });
      },
    });
    this.load(1);
  }

  error(name: 'categoryId' | 'nameEn' | 'sku' | 'price'): string | null {
    return firstErrorKey(this.form.controls[name]);
  }

  load(page: number): void {
    this.page = page;
    this.loading.set(true); this.failed.set(false);
    this.catalog.listProviderProducts({ pageNumber: page, pageSize: 20, searchTerm: this.search.value || null }).subscribe({
      next: (r) => {
        const pageData = readPage<ProductListItemDto>(r);
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
    this.catalog.createProviderProduct(value.categoryId, {
      name: { en: value.nameEn },
      sku: value.sku,
      price: Number(value.price),
      status: value.status,
    }).subscribe({
      next: () => { this.toast.success(this.i18n.t('ui.created')); this.showForm.set(false); this.form.reset({ price: 0, status: 1 }); this.busy.set(false); this.load(1); },
      error: (err) => { applyServerError(this.form, err); this.busy.set(false); },
    });
  }

  async remove(p: ProductListItemDto): Promise<void> {
    if (this.busy()) return;
    if (!(await this.confirm.ask())) return;
    this.busy.set(true);
    this.catalog.deleteProviderProduct(p.categoryId, p.id).pipe(finalize(() => this.busy.set(false))).subscribe({ next: () => this.load(this.page) });
  }

  upload(p: ProductListItemDto, ev: Event): void {
    const file = (ev.target as HTMLInputElement).files?.[0];
    if (!file || this.busy()) return;
    this.busy.set(true);
    this.catalog.uploadProviderProductImage(p.categoryId, p.id, file).pipe(finalize(() => this.busy.set(false))).subscribe({
      next: () => this.toast.success(this.i18n.t('ui.uploaded')),
    });
  }
}
