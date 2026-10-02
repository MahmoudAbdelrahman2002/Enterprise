import { IconComponent } from '../../../shared/components/icon/icon.component';
﻿import { finalize } from 'rxjs';
import { Component, effect, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PermissionGroupDto, PermissionItemDto, RoleDetailDto, RoleListItemDto } from '../../../core/models/domain.models';
import { ConfirmService } from '../../../core/services/confirm.service';
import { I18nService } from '../../../core/services/i18n.service';
import { RolesService } from '../../../core/services/roles.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { PROVIDER_PERMISSION_GROUPS, normalizePermissionGroups } from '../../../core/utils/permission-catalog';
import { readList } from '../../../core/utils/read-list';
import { applyServerError, firstErrorKey } from '../../../shared/forms/form-errors';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-provider-roles',
  standalone: true,
  imports: [IconComponent, ReactiveFormsModule, EmptyStateComponent, TranslatePipe],
  template: `
    <div class="toolbar">
      <h1 class="page-title">{{ 'nav.roles' | t }}</h1>
      @if (canCreate) {
        <button class="btn btn-primary" type="button" (click)="startCreate()">{{ 'actions.create' | t }}</button>
      }
    </div>
    @if (editing()) {
      <form class="card stack" [formGroup]="form" (ngSubmit)="save()">
        <div class="field">
          <label for="provider-roles-nameEn">{{ 'roles.nameEn' | t }}</label><input id="provider-roles-nameEn" formControlName="nameEn" />
          @if (nameError('nameEn'); as key) { <small class="field-error">{{ key | t }}</small> }
        </div>
        <div class="field">
          <label for="provider-roles-nameIt">{{ 'roles.nameIt' | t }}</label><input id="provider-roles-nameIt" formControlName="nameIt" />
        </div>
        <div class="field">
          <label for="provider-roles-nameAr">{{ 'roles.nameAr' | t }}</label><input id="provider-roles-nameAr" formControlName="nameAr" dir="rtl" />
        </div>
        @if (permError()) { <p class="field-error">{{ 'roles.noPermissions' | t }}</p> }
        @if (form.errors?.['server']) { <p class="field-error">{{ form.errors?.['server'] }}</p> }
        <p class="muted">{{ 'roles.hint.provider' | t }}</p>
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
                  [disabled]="editing()!.isSystem"
                />
                <span>
                  <strong>{{ permissionLabel(p) }}</strong>
                  <small class="muted">{{ p.action }} Â· {{ p.name }}</small>
                </span>
              </label>
            }
          </fieldset>
        }
        <div class="row">
          @if (!editing()!.isSystem) {
            <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'actions.save' | t }}</button>
          }
          <button class="btn btn-ghost" type="button" (click)="editing.set(null)">{{ 'actions.cancel' | t }}</button>
        </div>
      </form>
    } @else if (loading()) {
      <p class="muted">{{ 'loading' | t }}</p>
    } @else if (failed()) { <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost" type="button" (click)="reload()">{{ 'actions.retry' | t }}</button></div> } @else if (!items().length) {
      <app-empty-state />
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
                <td>{{ r.isSystem }}</td>
                <td class="row">
                  <button class="btn btn-ghost" type="button" (click)="edit(r)">{{ 'actions.edit' | t }}</button>
                  @if (canDelete && !r.isSystem) {
                    <button class="btn btn-danger" type="button" [disabled]="busy()" (click)="remove(r.id)"><app-icon name="trash" />{{ 'actions.delete' | t }}</button>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
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
export class ProviderRolesComponent {
  private readonly roles = inject(RolesService);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  private readonly i18n = inject(I18nService);
  private readonly confirm = inject(ConfirmService);
  readonly items = signal<RoleListItemDto[]>([]);
  readonly groups = signal<PermissionGroupDto[]>(PROVIDER_PERMISSION_GROUPS);
  readonly editing = signal<RoleDetailDto | null>(null);
  readonly loadingPerms = signal(false);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly busy = signal(false);
  readonly permError = signal(false);
  readonly form = inject(FormBuilder).nonNullable.group({
    nameEn: ['', Validators.required],
    nameIt: [''],
    nameAr: [''],
  });
  selected = new Set<string>();
  canCreate = this.tokens.hasPermission('provider', 'ProviderRoles.Create');
  canDelete = this.tokens.hasPermission('provider', 'ProviderRoles.Delete');

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
    this.roles.permissions('provider').subscribe({
      next: (g) => {
        const groups = normalizePermissionGroups(g);
        if (groups.length) this.groups.set(groups);
      },
    });
  }

  permissionLabel(p: PermissionItemDto): string {
    return p.description?.trim() || `${p.action} ${p.name}`;
  }

  reload(): void {
    this.loading.set(true); this.failed.set(false);
    this.roles.list('provider', { pageSize: 50 }).subscribe({
      next: (r) => { this.items.set(readList<RoleListItemDto>(r)); this.loading.set(false); },
      error: () => { this.failed.set(true); this.loading.set(false); },
    });
  }

  startCreate(): void {
    this.editing.set({
      id: '',
      name: '',
      names: { en: '' },
      roleType: 'Provider',
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
    this.roles.get('provider', r.id).subscribe({
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
    if (!current || current.isSystem) return;
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
      ? this.roles.update('provider', current.id, body)
      : this.roles.create('provider', body);
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
    this.roles.remove('provider', id).pipe(finalize(() => this.busy.set(false))).subscribe({ next: () => this.reload() });
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
