import { PageRequest } from '../../../core/utils/page-request';
import { fieldRules } from '../../../shared/forms/field-validators';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { Component, effect, inject, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { ClientProviderListItemDto } from '../../../core/models/domain.models';
import { CatalogService } from '../../../core/services/catalog.service';
import { I18nService } from '../../../core/services/i18n.service';
import { readPage, resolvePage } from '../../../core/utils/read-list';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { SearchFieldComponent } from '../../../shared/components/search-field/search-field.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-providers-list',
  standalone: true,
  imports: [IconComponent, RouterLink, ReactiveFormsModule, EmptyStateComponent, PaginationComponent, SearchFieldComponent, TranslatePipe],
  template: `
    <div class="toolbar">
      <div>
        <p class="muted eyebrow">{{ 'home.services' | t }}</p>
        <h1 class="page-title">{{ 'nav.providers' | t }}</h1>
      </div>
      <app-search-field [control]="search" placeholderKey="search.stores" />
    </div>

    @if (loading()) {
      <div class="grid-cards">
        @for (_ of [1, 2, 3, 4, 5, 6]; track $index) {
          <div class="card store-card">
            <div class="thumb-lg skeleton"></div>
            <div class="skeleton line"></div>
            <div class="skeleton line short"></div>
          </div>
        }
      </div>
    } @else if (failed()) {
      <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost" type="button" (click)="load(1)">{{ 'actions.retry' | t }}</button></div>
    } @else if (!items().length) {
      <app-empty-state />
    } @else {
      <div class="grid-cards">
        @for (p of items(); track p.id) {
          <a class="card store-card" [routerLink]="['/stores', p.id]">
            @if (p.imageUrl) {
              <img class="thumb-lg" [src]="p.imageUrl" [alt]="p.companyName" loading="lazy" />
            } @else {
              <div class="thumb-lg media-fallback">{{ p.companyName.slice(0, 1) }}</div>
            }
            <h3>{{ p.companyName }}</h3>
            <p class="muted">{{ p.serviceName }}</p>
            <span class="cta">{{ 'home.viewStore' | t }} <app-icon class="directional" name="right" /></span>
          </a>
        }
      </div>
    }
    <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" [disabled]="loading()" (change)="load($event)" />
  `,
  styles: [
    `
      .eyebrow {
        margin: 0 0 0.2rem;
        font-size: 0.8rem;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.04em;
      }
      .page-title {
        margin-bottom: 0;
      }
      .store-card {
        display: flex;
        flex-direction: column;
        gap: 0.45rem;
        padding: 0.85rem;
        transition: transform 0.15s ease, box-shadow 0.15s ease;
      }
      .store-card:hover {
        transform: translateY(-2px);
        box-shadow: var(--subito-shadow);
      }
      .store-card h3 {
        margin: 0.25rem 0 0;
        font-family: var(--subito-display);
        letter-spacing: -0.02em;
      }
      .store-card .muted {
        margin: 0;
      }
      .cta {
        margin-top: auto;
        font-weight: 800;
        color: var(--subito-navy);
        font-size: 0.9rem;
      }
      .line {
        height: 12px;
        margin-top: 0.65rem;
      }
      .line.short {
        width: 45%;
      }
    `,
  ],
})
export class ProvidersListComponent {
  private readonly pageRequest = new PageRequest(inject(DestroyRef));
  private readonly catalog = inject(CatalogService);
  private readonly route = inject(ActivatedRoute);
  private readonly i18n = inject(I18nService);
  readonly items = signal<ClientProviderListItemDto[]>([]);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly search = new FormControl('', { nonNullable: true, validators: fieldRules.search });
  page = 1;
  totalPages = 1;
  totalCount = 0;
  serviceId = '';

  constructor() {
    this.serviceId = this.route.snapshot.paramMap.get('serviceId') || '';
    this.search.valueChanges.pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed()).subscribe(() => { if (this.search.valid) this.load(1); });
    effect(() => {
      this.i18n.lang();
      this.load(1);
    });
  }

  load(page: number): void {
    if (!this.serviceId) return;
    this.page = page;
    this.loading.set(true); this.failed.set(false);
    this.pageRequest.run(this.catalog
      .listProvidersByService(this.serviceId, {
        pageNumber: page,
        pageSize: 12,
        searchTerm: this.search.value || null,
      }), {
        next: (res) => {
          const pageData = readPage<ClientProviderListItemDto>(res);
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
}
