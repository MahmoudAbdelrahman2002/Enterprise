import { Component, EventEmitter, Input, Output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { RoleListItemDto } from '../../../core/models/domain.models';
import { Query } from '../../../core/services/api.service';
import { fieldRules } from '../../forms/field-validators';
import { SearchFieldComponent } from '../search-field/search-field.component';
import { TranslatePipe } from '../../pipes/translate.pipe';

@Component({
  selector: 'app-team-filters', standalone: true,
  imports: [FormsModule, ReactiveFormsModule, SearchFieldComponent, TranslatePipe],
  template: `
    <div class="row team-filters">
      <app-search-field [control]="search" [placeholderKey]="kind === 'roles' ? 'search.roles' : 'search.team'" />
      @if (kind === 'staff') {
        <div class="field"><label for="team-status">{{ 'ui.status' | t }}</label>
          <select id="team-status" [(ngModel)]="status" (ngModelChange)="emit()">
            <option value="">{{ 'filters.allStatuses' | t }}</option><option value="active">{{ 'status.active' | t }}</option><option value="inactive">{{ 'status.inactive' | t }}</option>
          </select>
        </div>
        @if (roles.length) {
          <div class="field"><label for="team-role">{{ 'ui.role' | t }}</label>
            <select id="team-role" [(ngModel)]="roleId" (ngModelChange)="emit()"><option value="">{{ 'filters.allRoles' | t }}</option>
              @for (role of roles; track role.id) { <option [value]="role.id">{{ role.name }}</option> }
            </select>
          </div>
        }
      } @else {
        <div class="field"><label for="team-system">{{ 'roles.col.system' | t }}</label>
          <select id="team-system" [(ngModel)]="system" (ngModelChange)="emit()"><option value="">{{ 'filters.allRoles' | t }}</option><option value="system">{{ 'roles.systemRole' | t }}</option><option value="custom">{{ 'roles.customRole' | t }}</option></select>
        </div>
      }
      <div class="field"><label for="team-sort">{{ 'filters.sortName' | t }}</label>
        <select id="team-sort" [(ngModel)]="descending" (ngModelChange)="emit()"><option [ngValue]="false">{{ 'filters.ascending' | t }}</option><option [ngValue]="true">{{ 'filters.descending' | t }}</option></select>
      </div>
      <button class="btn btn-ghost" type="button" (click)="clear()">{{ 'actions.clear' | t }}</button>
    </div>
  `,
})
export class TeamFiltersComponent {
  @Input() kind: 'staff' | 'roles' = 'staff';
  @Input() roles: RoleListItemDto[] = [];
  @Output() changed = new EventEmitter<Query>();
  readonly search = new FormControl('', { nonNullable: true, validators: fieldRules.search });
  status = ''; roleId = ''; system = ''; descending = false;
  constructor() {
    this.search.valueChanges.pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed()).subscribe(() => this.emit());
  }
  emit(): void {
    if (this.search.invalid) return;
    this.changed.emit({ searchTerm: this.search.value.trim() || null,
      roleId: this.roleId || null, isActive: this.status ? this.status === 'active' : null,
      isSystem: this.system ? this.system === 'system' : null, descending: this.descending });
  }
  clear(): void { this.search.setValue('', { emitEvent: false }); this.status = ''; this.roleId = ''; this.system = ''; this.descending = false; this.emit(); }
}
