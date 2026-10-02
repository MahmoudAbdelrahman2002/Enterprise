import { ImageUploadComponent } from '../../../shared/components/image-upload/image-upload.component';
import { I18nService } from '../../../core/services/i18n.service';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { finalize } from 'rxjs';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CategoryDetailDto } from '../../../core/models/domain.models';
import { CatalogService } from '../../../core/services/catalog.service';
import { ConfirmService } from '../../../core/services/confirm.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { readPage } from '../../../core/utils/read-list';
import { applyServerError, firstErrorKey } from '../../../shared/forms/form-errors';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-provider-categories',
  standalone: true,
  imports: [ImageUploadComponent, IconComponent, ReactiveFormsModule, EmptyStateComponent, PaginationComponent, TranslatePipe],
  template: `
    <div class="toolbar">
      <h1 class="page-title">{{ 'nav.categories' | t }}</h1>
      @if (canCreate) { <button class="btn btn-primary" type="button" (click)="showForm.set(true)">{{ 'actions.create' | t }}</button> }
    </div>
    @if (showForm()) {
      <form class="card stack" [formGroup]="form" (ngSubmit)="create()">
        <div class="field"><label for="provider-categories-nameEn">{{ 'roles.nameEn' | t }}</label><input id="provider-categories-nameEn" formControlName="nameEn" />
          @if (error('nameEn'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="provider-categories-descEn">{{ 'ui.descriptionEn' | t }}</label><textarea id="provider-categories-descEn" formControlName="descEn"></textarea></div>
        <div class="field"><label for="provider-categories-displayOrder">{{ 'ui.displayOrder' | t }}</label><input id="provider-categories-displayOrder" type="number" formControlName="displayOrder" />
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
        <thead><tr><th>{{ 'ui.name' | t }}</th><th>{{ 'ui.displayOrder' | t }}</th><th>{{ 'ui.active' | t }}</th><th></th></tr></thead>
        <tbody>
          @for (c of items(); track c.id) {
            <tr>
              <td>{{ c.name }}</td><td>{{ c.displayOrder }}</td>
              <td><span class="badge" [class.badge-success]="c.isActive">{{ (c.isActive ? 'status.active' : 'status.inactive') | t }}</span></td>
              <td class="row">
                @if (canUpdate) {
                  <button class="btn btn-ghost" type="button" (click)="toggle(c)">{{ c.isActive ? ('actions.deactivate'|t) : ('actions.activate'|t) }}</button>
                  <app-image-upload [disabled]="busy()" (selected)="upload(c.id, $event)" />
                }
                @if (canDelete) { <button class="btn btn-danger" type="button" [disabled]="busy()" (click)="remove(c.id)"><app-icon name="trash" />{{ 'actions.delete' | t }}</button> }
              </td>
            </tr>
          }
        </tbody>
      </table></div>
      <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" (change)="load($event)" />
    }
  `,
})
export class ProviderCategoriesComponent implements OnInit {
  private readonly i18n = inject(I18nService);
  private readonly catalog = inject(CatalogService);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  private readonly confirm = inject(ConfirmService);
  readonly items = signal<CategoryDetailDto[]>([]);
  readonly showForm = signal(false);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly busy = signal(false);
  page = 1; totalPages = 1; totalCount = 0;
  readonly form = inject(FormBuilder).nonNullable.group({
    nameEn: ['', Validators.required],
    descEn: [''],
    displayOrder: [0, [Validators.required, Validators.min(0)]],
  });
  canCreate = this.tokens.hasPermission('provider', 'ProviderCategory.Create');
  canUpdate = this.tokens.hasPermission('provider', 'ProviderCategory.Update');
  canDelete = this.tokens.hasPermission('provider', 'ProviderCategory.Delete');

  ngOnInit(): void { this.load(1); }

  error(name: 'nameEn' | 'displayOrder'): string | null {
    return firstErrorKey(this.form.controls[name]);
  }

  load(page: number): void {
    this.page = page;
    this.loading.set(true); this.failed.set(false);
    this.catalog.listProviderCategories({ pageNumber: page, pageSize: 20 }).subscribe({
      next: (r) => {
        const pageData = readPage<CategoryDetailDto>(r);
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
    this.catalog.createProviderCategory({
      displayOrder: Number(value.displayOrder),
      isActive: true,
      name: { en: value.nameEn },
      description: { en: value.descEn || null },
    }).subscribe({
      next: () => { this.toast.success(this.i18n.t('ui.created')); this.showForm.set(false); this.form.reset({ displayOrder: 0 }); this.busy.set(false); this.load(1); },
      error: (err) => { applyServerError(this.form, err); this.busy.set(false); },
    });
  }

  toggle(c: CategoryDetailDto): void {
    this.catalog.setProviderCategoryActive(c.id, !c.isActive).subscribe({ next: () => this.load(this.page) });
  }

  async remove(id: string): Promise<void> {
    if (this.busy()) return;
    if (!(await this.confirm.ask())) return;
    this.busy.set(true);
    this.catalog.deleteProviderCategory(id).pipe(finalize(() => this.busy.set(false))).subscribe({ next: () => this.load(this.page) });
  }

  upload(id: string, ev: Event): void {
    const file = (ev.target as HTMLInputElement).files?.[0];
    if (!file || this.busy()) return;
    this.busy.set(true);
    this.catalog.uploadProviderCategoryImage(id, file).pipe(finalize(() => this.busy.set(false))).subscribe({ next: () => this.toast.success(this.i18n.t('ui.uploaded')) });
  }
}
