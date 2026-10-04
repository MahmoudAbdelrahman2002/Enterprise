import { PageRequest } from '../../../core/utils/page-request';
import { fieldRules } from '../../../shared/forms/field-validators';
import { ActiveToggleComponent } from '../../../shared/components/active-toggle/active-toggle.component';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { AdminClientDto } from '../../../core/models/domain.models';
import { ProvidersService } from '../../../core/services/providers.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { readPage, resolvePage } from '../../../core/utils/read-list';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { SearchFieldComponent } from '../../../shared/components/search-field/search-field.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-clients',
  standalone: true,
  imports: [ActiveToggleComponent, IconComponent, ReactiveFormsModule, EmptyStateComponent, PaginationComponent, SearchFieldComponent, TranslatePipe],
  template: `
    <div class="toolbar">
      <h1 class="page-title">{{ 'nav.clients' | t }}</h1>
      <app-search-field [control]="search" placeholderKey="search.clients" />
    </div>
    @if (loading()) { <p class="muted">{{ 'loading' | t }}</p> }
    @else if (failed()) { <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost icon-action" type="button" (click)="load(1)" [attr.aria-label]="'actions.retry' | t" [title]="'actions.retry' | t"><app-icon name="retry" /></button></div> } @else if (!items().length) { <app-empty-state /> } @else {
      <div class="table-wrap card"><table class="data">
        <thead><tr><th>{{ 'ui.name' | t }}</th><th>{{ 'auth.email' | t }}</th><th>{{ 'ui.active' | t }}</th><th></th></tr></thead>
        <tbody>
          @for (c of items(); track c.id) {
            <tr>
              <td>{{ c.firstName }} {{ c.lastName }}</td><td>{{ c.email }}</td><td>@if (canUpdate) { <app-active-toggle [targetName]="c.firstName + ' ' + c.lastName" confirmationKey="confirm.deactivateAccount" [active]="c.isActive" [disabled]="busy()" (changed)="toggle(c)" /> } @else { <span class="badge">{{ (c.isActive ? 'status.active' : 'status.inactive') | t }}</span> }</td>
              <td>
              </td>
            </tr>
          }
        </tbody>
      </table></div>
    }
    <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" [disabled]="loading() || busy()" (change)="load($event)" />
  `,
})
export class AdminClientsComponent implements OnInit {
  private readonly pageRequest = new PageRequest(inject(DestroyRef));
  private readonly providers = inject(ProvidersService);
  private readonly tokens = inject(TokenStoreService);
  readonly items = signal<AdminClientDto[]>([]);
  readonly busy = signal(false);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly search = new FormControl('', { nonNullable: true, validators: fieldRules.search });
  page = 1; totalPages = 1; totalCount = 0;
  canUpdate = this.tokens.hasPermission('admin', 'Clients.Update');

  constructor() {
    this.search.valueChanges.pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed()).subscribe(() => { if (this.search.valid) this.load(1); });
  }

  ngOnInit(): void { this.load(1); }

  load(page: number): void {
    this.page = page;
    this.loading.set(true); this.failed.set(false);
    this.pageRequest.run(this.providers.listClients({ pageNumber: page, pageSize: 20, searchTerm: this.search.value || null }), {
      next: (r) => {
        const pageData = readPage<AdminClientDto>(r);
        const targetPage = resolvePage(page, pageData);
        if (page !== targetPage) { this.load(targetPage); return; }
        this.items.set(pageData.items);
        this.totalPages = pageData.totalPages;
        this.totalCount = pageData.totalCount;
        this.loading.set(false);
      },
      error: () => { this.failed.set(true); this.loading.set(false); },
    });
  }

  toggle(c: AdminClientDto): void {
    if (this.busy()) return;
    this.busy.set(true);
    this.providers.setClientActive(c.id, !c.isActive).subscribe({
      next: () => { this.load(this.page); this.busy.set(false); },
      error: () => this.busy.set(false),
    });
  }
}
