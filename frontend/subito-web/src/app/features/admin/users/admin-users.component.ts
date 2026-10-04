import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PageRequest } from '../../../core/utils/page-request';
import { TeamFiltersComponent } from '../../../shared/components/team-filters/team-filters.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { Query } from '../../../core/services/api.service';
import { FieldValidationDirective } from '../../../shared/directives/field-validation.directive';
import { fieldRules } from '../../../shared/forms/field-validators';
import { ActiveToggleComponent } from '../../../shared/components/active-toggle/active-toggle.component';
import { PasswordToggleDirective } from '../../../shared/directives/password-toggle.directive';
import { I18nService } from '../../../core/services/i18n.service';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { finalize } from 'rxjs';
import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RoleListItemDto, StaffListItemDto } from '../../../core/models/domain.models';
import { ConfirmService } from '../../../core/services/confirm.service';
import { RolesService } from '../../../core/services/roles.service';
import { StaffService } from '../../../core/services/staff.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { readList, readPage, resolvePage } from '../../../core/utils/read-list';
import { applyServerError, firstErrorKey, strongPassword } from '../../../shared/forms/form-errors';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [TeamFiltersComponent, PaginationComponent, FieldValidationDirective, ActiveToggleComponent, PasswordToggleDirective, IconComponent, ReactiveFormsModule, EmptyStateComponent, TranslatePipe],
  template: `
    <div class="toolbar">
      <h1 class="page-title">{{ 'nav.users' | t }}</h1>
      @if (canCreate && !showForm()) { <button class="btn btn-primary icon-action" type="button" (click)="showForm.set(true)" [attr.aria-label]="'actions.create' | t" [title]="'actions.create' | t"><app-icon name="plus" />{{ 'actions.create' | t }}</button> }
    </div>
    @if (!showForm()) { <app-team-filters kind="staff" [roles]="roles()" (changed)="applyFilters($event)" /> }
    @if (showForm()) {
      <form class="card stack" [formGroup]="form" (ngSubmit)="create()">
        <div class="field"><label for="admin-users-firstName">{{ 'auth.firstName' | t }}</label><input id="admin-users-firstName" appFieldValidation formControlName="firstName" /></div>
        <div class="field"><label for="admin-users-lastName">{{ 'auth.lastName' | t }}</label><input id="admin-users-lastName" appFieldValidation formControlName="lastName" /></div>
        <div class="field"><label for="admin-users-email">{{ 'auth.email' | t }}</label><input id="admin-users-email" type="email" appFieldValidation formControlName="email" autocomplete="email" /></div>
        <div class="field"><label for="admin-users-password">{{ 'auth.password' | t }}</label><input id="admin-users-password" type="password" appPasswordToggle appFieldValidation formControlName="password" autocomplete="current-password" /></div>
        <div class="field"><label for="admin-users-roleId">{{ 'ui.role' | t }}</label><select id="admin-users-roleId" appFieldValidation formControlName="roleId">
            <option value="">—</option>
            @for (r of roles(); track r.id) { <option [value]="r.id">{{ r.name }}</option> }
          </select>
        </div>
        @if (form.errors?.['server']) { <p class="field-error">{{ form.errors?.['server'] }}</p> }
        <div class="row">
          <button class="btn btn-primary icon-action" type="submit" [disabled]="busy()" [attr.aria-label]="'actions.save' | t" [title]="'actions.save' | t"><app-icon name="check" />{{ 'actions.save' | t }}</button>
          <button class="btn btn-ghost icon-action" type="button" (click)="showForm.set(false)" [attr.aria-label]="'actions.cancel' | t" [title]="'actions.cancel' | t"><app-icon name="close" />{{ 'actions.cancel' | t }}</button>
        </div>
      </form>
    }
    @if (!showForm()) {
    @if (loading()) { <p class="muted">{{ 'loading' | t }}</p> }
    @else if (failed()) { <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost icon-action" type="button" (click)="reload()" [attr.aria-label]="'actions.retry' | t" [title]="'actions.retry' | t"><app-icon name="retry" /></button></div> } @else if (!items().length) { <app-empty-state [messageKey]="hasFilters ? 'empty.teamSearch' : 'empty.staff'" /> } @else {
      <div class="table-wrap card"><table class="data">
        <thead><tr><th>{{ 'ui.name' | t }}</th><th>{{ 'auth.email' | t }}</th><th>{{ 'ui.role' | t }}</th><th>{{ 'ui.active' | t }}</th><th></th></tr></thead>
        <tbody>
          @for (u of items(); track u.id) {
            <tr>
              <td>{{ u.firstName }} {{ u.lastName }}</td><td>{{ u.email }}</td><td>{{ u.roleName }}</td><td>@if (canUpdate && !u.isSystem) { <app-active-toggle [targetName]="u.firstName + ' ' + u.lastName" confirmationKey="confirm.deactivateAccount" [active]="u.isActive" [disabled]="busy()" (changed)="toggle(u)" /> } @else { <span class="badge">{{ (u.isActive ? 'status.active' : 'status.inactive') | t }}</span> }</td>
              <td class="row">
                @if (canDelete && !u.isSystem) {
                  <button class="btn btn-danger icon-action" type="button" [disabled]="busy()" (click)="remove(u.id)" [attr.aria-label]="'actions.delete' | t" [title]="'actions.delete' | t"><app-icon name="trash" /></button>
                }
              </td>
            </tr>
          }
        </tbody>
      </table></div>
    }
    }
    @if (!showForm()) { <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" [disabled]="loading() || busy()" (change)="reload($event)" /> }
  `,
})
export class AdminUsersComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly pageRequest = new PageRequest(this.destroyRef);
  private readonly i18n = inject(I18nService);
  private readonly staff = inject(StaffService);
  private readonly rolesApi = inject(RolesService);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  private readonly confirm = inject(ConfirmService);
  readonly items = signal<StaffListItemDto[]>([]);
  readonly roles = signal<RoleListItemDto[]>([]);
  readonly showForm = signal(false);
  page = 1; totalPages = 1; totalCount = 0;
  filters: Query = {};
  get hasFilters(): boolean { return Boolean(this.filters["searchTerm"] || this.filters["roleId"] || this.filters["isActive"] != null || this.filters["isSystem"] != null); }
  applyFilters(filters: Query): void { this.filters = filters; this.reload(1); }
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly busy = signal(false);
  readonly form = inject(FormBuilder).nonNullable.group({
    firstName: ['', fieldRules.staffName],
    lastName: ['', fieldRules.staffName],
    email: ['', fieldRules.email],
    password: ['', fieldRules.password],
    roleId: ['', fieldRules.id],
    phoneNumber: ['', fieldRules.phone],
  });
  canCreate = this.tokens.hasPermission('admin', 'Admins.Create');
  canUpdate = this.tokens.hasPermission('admin', 'Admins.Update');
  canDelete = this.tokens.hasPermission('admin', 'Admins.Delete');

  ngOnInit(): void {
    if (this.tokens.hasPermission('admin', 'Roles.Read')) this.rolesApi.lookup('admin').pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: (r) => this.roles.set(readList<RoleListItemDto>(r)) });
    this.reload();
  }

  error(name: 'firstName' | 'lastName' | 'email' | 'password' | 'roleId'): string | null {
    return firstErrorKey(this.form.controls[name]);
  }

  reload(page = this.page): void {
    this.page = page;
    this.loading.set(true); this.failed.set(false);
    this.pageRequest.run(this.staff.listAdmin({ ...this.filters, pageNumber: page, pageSize: 20 }), {
      next: (r) => { const data = readPage<StaffListItemDto>(r);
        const targetPage = resolvePage(page, data);
        if (page !== targetPage) { this.reload(targetPage); return; } this.items.set(data.items); this.totalPages = data.totalPages; this.totalCount = data.totalCount; this.loading.set(false); },
      error: () => { this.failed.set(true); this.loading.set(false); },
    });
  }

  create(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.busy.set(true);
    this.staff.createAdmin(this.form.getRawValue()).subscribe({
      next: () => { this.toast.success(this.i18n.t('ui.created')); this.showForm.set(false); this.form.reset(); this.busy.set(false); this.reload(); },
      error: (err) => { applyServerError(this.form, err); this.busy.set(false); },
    });
  }

  toggle(u: StaffListItemDto): void {
    if (this.busy()) return;
    this.busy.set(true);
    this.staff.setAdminActive(u.id, !u.isActive).subscribe({
      next: () => { this.reload(); this.busy.set(false); },
      error: () => this.busy.set(false),
    });
  }

  async remove(id: string): Promise<void> {
    if (this.busy()) return;
    if (!(await this.confirm.ask())) return;
    this.busy.set(true);
    this.staff.deleteAdmin(id).pipe(finalize(() => this.busy.set(false))).subscribe({ next: () => this.reload() });
  }
}
