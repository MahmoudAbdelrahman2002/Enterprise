import { I18nService } from '../../../core/services/i18n.service';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { finalize } from 'rxjs';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RoleListItemDto, StaffListItemDto } from '../../../core/models/domain.models';
import { ConfirmService } from '../../../core/services/confirm.service';
import { RolesService } from '../../../core/services/roles.service';
import { StaffService } from '../../../core/services/staff.service';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { readList } from '../../../core/utils/read-list';
import { applyServerError, firstErrorKey, strongPassword } from '../../../shared/forms/form-errors';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-provider-staff',
  standalone: true,
  imports: [IconComponent, ReactiveFormsModule, EmptyStateComponent, TranslatePipe],
  template: `
    <div class="toolbar">
      <h1 class="page-title">{{ 'nav.staff' | t }}</h1>
      @if (canCreate) { <button class="btn btn-primary" type="button" (click)="showForm.set(true)">{{ 'actions.create' | t }}</button> }
    </div>
    @if (showForm()) {
      <form class="card stack" [formGroup]="form" (ngSubmit)="create()">
        <div class="field"><label for="provider-staff-firstName">{{ 'auth.firstName' | t }}</label><input id="provider-staff-firstName" formControlName="firstName" />
          @if (error('firstName'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="provider-staff-lastName">{{ 'auth.lastName' | t }}</label><input id="provider-staff-lastName" formControlName="lastName" />
          @if (error('lastName'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="provider-staff-email">{{ 'auth.email' | t }}</label><input id="provider-staff-email" type="email" formControlName="email" autocomplete="email" />
          @if (error('email'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="provider-staff-password">{{ 'auth.password' | t }}</label><input id="provider-staff-password" type="password" formControlName="password" autocomplete="current-password" />
          @if (error('password'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="provider-staff-roleId">{{ 'ui.role' | t }}</label><select id="provider-staff-roleId" formControlName="roleId">
            <option value="">—</option>
            @for (r of roles(); track r.id) { <option [value]="r.id">{{ r.name }}</option> }
          </select>
          @if (error('roleId'); as key) { <small class="field-error">{{ key | t }}</small> }
        </div>
        @if (form.errors?.['server']) { <p class="field-error">{{ form.errors?.['server'] }}</p> }
        <div class="row">
          <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'actions.save' | t }}</button>
          <button class="btn btn-ghost" type="button" (click)="showForm.set(false)">{{ 'actions.cancel' | t }}</button>
        </div>
      </form>
    }
    @if (loading()) { <p class="muted">{{ 'loading' | t }}</p> }
    @else if (failed()) { <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost" type="button" (click)="reload()">{{ 'actions.retry' | t }}</button></div> } @else if (!items().length) { <app-empty-state /> } @else {
      <div class="table-wrap card"><table class="data">
        <thead><tr><th>{{ 'ui.name' | t }}</th><th>{{ 'auth.email' | t }}</th><th>{{ 'ui.role' | t }}</th><th>{{ 'ui.active' | t }}</th><th></th></tr></thead>
        <tbody>
          @for (s of items(); track s.id) {
            <tr>
              <td>{{ s.firstName }} {{ s.lastName }}</td><td>{{ s.email }}</td><td>{{ s.roleName }}</td>
              <td><span class="badge">{{ (s.isActive ? 'status.active' : 'status.inactive') | t }}</span></td>
              <td class="row">
                @if (canUpdate && !s.isSystem) {
                  <button class="btn btn-ghost" type="button" (click)="toggle(s)">{{ s.isActive ? ('actions.deactivate'|t) : ('actions.activate'|t) }}</button>
                }
                @if (canDelete && !s.isSystem) {
                  <button class="btn btn-danger" type="button" [disabled]="busy()" (click)="remove(s.id)"><app-icon name="trash" />{{ 'actions.delete' | t }}</button>
                }
              </td>
            </tr>
          }
        </tbody>
      </table></div>
    }
  `,
})
export class ProviderStaffComponent implements OnInit {
  private readonly i18n = inject(I18nService);
  private readonly staff = inject(StaffService);
  private readonly rolesApi = inject(RolesService);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  private readonly confirm = inject(ConfirmService);
  readonly items = signal<StaffListItemDto[]>([]);
  readonly roles = signal<RoleListItemDto[]>([]);
  readonly showForm = signal(false);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly busy = signal(false);
  readonly form = inject(FormBuilder).nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, strongPassword]],
    roleId: ['', Validators.required],
    phoneNumber: [''],
  });
  canCreate = this.tokens.hasPermission('provider', 'ProviderStaff.Create');
  canUpdate = this.tokens.hasPermission('provider', 'ProviderStaff.Update');
  canDelete = this.tokens.hasPermission('provider', 'ProviderStaff.Delete');

  ngOnInit(): void {
    this.rolesApi.list('provider', { pageSize: 100 }).subscribe({ next: (r) => this.roles.set(readList<RoleListItemDto>(r)) });
    this.reload();
  }

  error(name: 'firstName' | 'lastName' | 'email' | 'password' | 'roleId'): string | null {
    return firstErrorKey(this.form.controls[name]);
  }

  reload(): void {
    this.loading.set(true); this.failed.set(false);
    this.staff.listProvider({ pageSize: 50 }).subscribe({
      next: (r) => { this.items.set(readList<StaffListItemDto>(r)); this.loading.set(false); },
      error: () => { this.failed.set(true); this.loading.set(false); },
    });
  }

  create(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.busy.set(true);
    this.staff.createProvider(this.form.getRawValue()).subscribe({
      next: () => { this.toast.success(this.i18n.t('ui.created')); this.showForm.set(false); this.form.reset(); this.busy.set(false); this.reload(); },
      error: (err) => { applyServerError(this.form, err); this.busy.set(false); },
    });
  }

  toggle(s: StaffListItemDto): void {
    this.staff.setProviderActive(s.id, !s.isActive).subscribe({ next: () => this.reload() });
  }

  async remove(id: string): Promise<void> {
    if (this.busy()) return;
    if (!(await this.confirm.ask())) return;
    this.busy.set(true);
    this.staff.deleteProvider(id).pipe(finalize(() => this.busy.set(false))).subscribe({ next: () => this.reload() });
  }
}
