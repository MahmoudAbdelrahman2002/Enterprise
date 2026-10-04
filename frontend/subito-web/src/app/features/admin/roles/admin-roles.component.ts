import { PageRequest } from '../../../core/utils/page-request';
import { TeamFiltersComponent } from '../../../shared/components/team-filters/team-filters.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { Query } from '../../../core/services/api.service';
import { FieldValidationDirective } from '../../../shared/directives/field-validation.directive';
import { fieldRules } from '../../../shared/forms/field-validators';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { finalize } from 'rxjs';
import { Component, effect, inject, signal, DestroyRef } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PermissionGroupDto, PermissionItemDto, RoleDetailDto, RoleListItemDto } from '../../../core/models/domain.models';
import { ConfirmService } from '../../../core/services/confirm.service';
import { I18nService } from '../../../core/services/i18n.service';
import { RolesService } from '../../../core/services/roles.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { ADMIN_PERMISSION_GROUPS, normalizePermissionGroups } from '../../../core/utils/permission-catalog';
import { readList, readPage, resolvePage } from '../../../core/utils/read-list';
import { applyServerError, firstErrorKey } from '../../../shared/forms/form-errors';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-roles',
  standalone: true,
  imports: [TeamFiltersComponent, PaginationComponent, FieldValidationDirective, IconComponent, ReactiveFormsModule, EmptyStateComponent, TranslatePipe],
  template: `
    <div class="toolbar">
      <h1 class="page-title">{{ 'nav.roles' | t }}</h1>
      @if (canCreate) {
        <button class="btn btn-primary icon-action" type="button" (click)="startCreate()" [attr.aria-label]="'actions.create' | t" [title]="'actions.create' | t"><app-icon name="plus" />{{ 'actions.create' | t }}</button>
      }
    </div>
    @if (!editing()) { <app-team-filters kind="roles" (changed)="applyFilters($event)" /> }
    @if (editing()) {
      <form class="card stack" [formGroup]="form" (ngSubmit)="save()">
        <div class="field">
          <label for="admin-roles-nameEn">{{ 'roles.nameEn' | t }}</label><input id="admin-roles-nameEn" appFieldValidation formControlName="nameEn" />
        </div>
        <div class="field">
          <label for="admin-roles-nameIt">{{ 'roles.nameIt' | t }}</label><input id="admin-roles-nameIt" appFieldValidation formControlName="nameIt" />
        </div>
        <div class="field">
          <label for="admin-roles-nameAr">{{ 'roles.nameAr' | t }}</label><input id="admin-roles-nameAr" appFieldValidation formControlName="nameAr" dir="rtl" />
        </div>
        @if (permError()) { <p class="field-error">{{ 'roles.noPermissions' | t }}</p> }
        @if (form.errors?.['server']) { <p class="field-error">{{ form.errors?.['server'] }}</p> }
        <p class="muted">{{ 'roles.hint.admin' | t }}</p>
        @if (loadingPerms()) {
          <p class="muted">{{ 'loading' | t }}</p>
        } @else if (!groups().length) {
          <p class="muted">{{ 'roles.noPermissions' | t }}</p>
        }
        @for (g of groups(); track g.module) {
          <fieldset>
            <legend>{{ g.moduleLabel || g.module }}</legend>
            @for (p of g.permissions; track p.name) {
              <label class="perm-row">
                <input
                  type="checkbox"
                  [checked]="selected.has(p.name)"
                  (change)="toggle(p.name, $event)"
                  [disabled]="editing()!.isSystem || (editing()!.id ? !canUpdate : !canCreate)"
                />
                <span>
                  <strong>{{ permissionLabel(p) }}</strong>
                  <small class="muted">{{ p.action }} · {{ p.name }}</small>
                </span>
              </label>
            }
          </fieldset>
        }
        <div class="row">
          @if (!editing()!.isSystem && (editing()!.id ? canUpdate : canCreate)) {
            <button class="btn btn-primary icon-action" type="submit" [disabled]="busy()" [attr.aria-label]="'actions.save' | t" [title]="'actions.save' | t"><app-icon name="check" />{{ 'actions.save' | t }}</button>
          }
          <button class="btn btn-ghost icon-action" type="button" (click)="editing.set(null)" [attr.aria-label]="'actions.cancel' | t" [title]="'actions.cancel' | t"><app-icon name="close" />{{ 'actions.cancel' | t }}</button>
        </div>
      </form>
    } @else if (loading()) {
      <p class="muted">{{ 'loading' | t }}</p>
    } @else if (failed()) { <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost icon-action" type="button" (click)="reload()" [attr.aria-label]="'actions.retry' | t" [title]="'actions.retry' | t"><app-icon name="retry" /></button></div> } @else if (!items().length) {
      <app-empty-state [messageKey]="hasFilters ? 'empty.teamSearch' : 'empty.roles'" />
    } @else {
      <div class="table-wrap card">
        <table class="data">
          <thead>
            <tr>
              <th>{{ 'roles.col.name' | t }}</th>
              <th>{{ 'roles.col.users' | t }}</th>
              <th>{{ 'roles.col.system' | t }}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (r of items(); track r.id) {
              <tr>
                <td>{{ r.name }}</td>
                <td>{{ r.usersCount }}</td>
                <td>{{ (r.isSystem ? 'roles.systemRole' : 'roles.customRole') | t }}</td>
                <td class="row">
                  <button class="btn btn-ghost icon-action" type="button" (click)="edit(r)" [attr.aria-label]="'actions.edit' | t" [title]="'actions.edit' | t"><app-icon name="edit" /></button>
                  @if (canDelete && !r.isSystem) {
                    <button class="btn btn-danger icon-action" type="button" [disabled]="busy()" (click)="remove(r.id)" [attr.aria-label]="'actions.delete' | t" [title]="'actions.delete' | t"><app-icon name="trash" /></button>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
    @if (!editing()) { <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" [disabled]="loading() || busy()" (change)="reload($event)" /> }
  `,
  styles: [
    `
      fieldset {
        border: 1px solid var(--subito-border);
        border-radius: 12px;
        padding: 0.75rem 1rem;
      }
      .perm-row {
        display: flex;
        gap: 0.65rem;
        align-items: flex-start;
        margin: 0.45rem 0;
      }
      .perm-row span {
        display: flex;
        flex-direction: column;
        gap: 0.1rem;
      }
    `,
  ],
})
export class AdminRolesComponent {
  private readonly pageRequest = new PageRequest(inject(DestroyRef));
  private readonly roles = inject(RolesService);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  private readonly i18n = inject(I18nService);
  private readonly confirm = inject(ConfirmService);
  readonly items = signal<RoleListItemDto[]>([]);
  readonly groups = signal<PermissionGroupDto[]>(ADMIN_PERMISSION_GROUPS);
  readonly editing = signal<RoleDetailDto | null>(null);
  readonly loadingPerms = signal(false);
  page = 1; totalPages = 1; totalCount = 0;
  filters: Query = {};
  get hasFilters(): boolean { return Boolean(this.filters["searchTerm"] || this.filters["roleId"] || this.filters["isActive"] != null || this.filters["isSystem"] != null); }
  applyFilters(filters: Query): void { this.filters = filters; this.reload(1); }
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly busy = signal(false);
  readonly permError = signal(false);
  readonly form = inject(FormBuilder).nonNullable.group({
    nameEn: ['', fieldRules.roleName],
    nameIt: ['', fieldRules.optionalRoleName],
    nameAr: ['', fieldRules.optionalRoleName],
  });
  selected = new Set<string>();
  readonly canUpdate = this.tokens.hasPermission('admin', 'Roles.Update');
  canCreate = this.tokens.hasPermission('admin', 'Roles.Create');
  canDelete = this.tokens.hasPermission('admin', 'Roles.Delete');

  constructor() {
    effect(() => {
      this.i18n.lang();
      this.loadPermissions();
      this.reload();
    });
  }

  nameError(name: 'nameEn'): string | null {
    return firstErrorKey(this.form.controls[name]);
  }

  loadPermissions(): void {
    this.roles.permissions('admin').subscribe({
      next: (g) => {
        const groups = normalizePermissionGroups(g);
        if (groups.length) this.groups.set(groups);
      },
    });
  }

  permissionLabel(p: PermissionItemDto): string {
    return p.description?.trim() || `${p.action} ${p.name}`;
  }

  reload(page = this.page): void {
    this.page = page;
    this.loading.set(true); this.failed.set(false);
    this.pageRequest.run(this.roles.list('admin', { ...this.filters, pageNumber: page, pageSize: 20 }), {
      next: (r) => { const data = readPage<RoleListItemDto>(r);
        const targetPage = resolvePage(page, data);
        if (page !== targetPage) { this.reload(targetPage); return; } this.items.set(data.items); this.totalPages = data.totalPages; this.totalCount = data.totalCount; this.loading.set(false); },
      error: () => { this.failed.set(true); this.loading.set(false); },
    });
  }

  startCreate(): void {
    if (!this.canCreate || this.busy()) return;
    this.editing.set({
      id: '',
      name: '',
      names: { en: '' },
      roleType: 'Admin',
      providerId: null,
      isSystem: false,
      usersCount: 0,
      permissions: [],
    });
    this.form.reset();
    this.form.enable();
    this.permError.set(false);
    this.selected = new Set();
    if (!this.groups().length) this.loadPermissions();
  }

  edit(r: RoleListItemDto): void {
    this.roles.get('admin', r.id).subscribe({
      next: (d) => {
        this.editing.set(d);
        this.applyNames(d);
        this.selected = new Set(d.permissions ?? (d as unknown as { Permissions?: string[] }).Permissions ?? []);
        if (d.isSystem) this.form.disable();
        else this.form.enable();
      },
    });
  }

  toggle(name: string, ev: Event): void {
    const checked = (ev.target as HTMLInputElement).checked;
    if (checked) this.selected.add(name);
    else this.selected.delete(name);
  }

  save(): void {
    const current = this.editing();
    if (!current || current.isSystem || this.busy() || (current.id ? !this.canUpdate : !this.canCreate)) return;
    this.form.markAllAsTouched();
    this.permError.set(this.selected.size === 0);
    if (this.form.invalid || this.selected.size === 0) return;
    const value = this.form.getRawValue();
    const body = {
      name: { en: value.nameEn.trim(), it: value.nameIt.trim() || null, ar: value.nameAr.trim() || null },
      permissions: [...this.selected],
    };
    this.busy.set(true);
    const req = current.id
      ? this.roles.update('admin', current.id, body)
      : this.roles.create('admin', body);
    req.subscribe({
      next: () => {
        this.toast.success(this.i18n.t('ui.saved'));
        this.editing.set(null);
        this.busy.set(false);
        this.reload();
      },
      error: (err) => { applyServerError(this.form, err); this.busy.set(false); },
    });
  }

  async remove(id: string): Promise<void> {
    if (this.busy()) return;
    if (!(await this.confirm.ask())) return;
    this.busy.set(true);
    this.roles.remove('admin', id).pipe(finalize(() => this.busy.set(false))).subscribe({ next: () => this.reload() });
  }

  private applyNames(d: RoleDetailDto): void {
    const names = d.names ?? (d as unknown as { Names?: RoleDetailDto['names'] }).Names;
    this.form.patchValue({
      nameEn: names?.en || d.name || '',
      nameIt: names?.it ?? '',
      nameAr: names?.ar ?? '',
    });
  }
}
