import { TokenStoreService } from '../../../core/services/token-store.service';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { CatalogService } from '../../../core/services/catalog.service';
import { OrdersService } from '../../../core/services/orders.service';
import { readPage } from '../../../core/utils/read-list';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-provider-home',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  template: `
    <section class="dash-hero card">
      <div>
        <p class="eyebrow">{{ 'nav.provider' | t }}</p>
        <h1 class="page-title">{{ 'dashboard.providerTitle' | t }}</h1>
        <p class="muted">{{ 'dashboard.providerHint' | t }}</p>
      </div>
    </section>

    @if (loading()) {
      <div class="stat-grid">
        @for (_ of [1, 2, 3]; track $index) {
          <div class="stat-card skeleton" style="height:110px"></div>
        }
      </div>
    } @else {
      <div class="stat-grid">
        @if (tokens.hasPermission('provider', 'ProviderProduct.Read')) { <a class="stat-card" routerLink="/provider/products">
          <span class="stat-label">{{ 'nav.products' | t }}</span>
          <strong class="stat-value">{{ counts().products }}</strong>
        </a> }
        @if (tokens.hasPermission('provider', 'ProviderCategory.Read')) { <a class="stat-card" routerLink="/provider/categories">
          <span class="stat-label">{{ 'nav.categories' | t }}</span>
          <strong class="stat-value">{{ counts().categories }}</strong>
        </a> }
        @if (tokens.hasPermission('provider', 'ProviderOrder.Read')) { <a class="stat-card" routerLink="/provider/orders">
          <span class="stat-label">{{ 'nav.orders' | t }}</span>
          <strong class="stat-value">{{ counts().orders }}</strong>
        </a> }
      </div>
    }
  `,
  styles: [
    `
      .dash-hero {
        margin-bottom: 1rem;
        background:
          radial-gradient(circle at top right, rgba(0, 199, 177, 0.16), transparent 45%),
          #fff;
        border-color: #d7efe9;
      }
      .eyebrow {
        margin: 0 0 0.35rem;
        font-size: 0.78rem;
        font-weight: 800;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        color: var(--subito-teal-dark);
      }
      .page-title {
        margin-bottom: 0.35rem;
      }
      .stat-card {
        display: flex;
        flex-direction: column;
        gap: 0.55rem;
        padding: 1.15rem 1.2rem;
        border-radius: var(--subito-radius);
        background: #fff;
        border: 1px solid var(--subito-border);
        box-shadow: var(--subito-shadow-sm);
        transition: transform 0.15s ease, box-shadow 0.15s ease, border-color 0.15s ease;
        position: relative;
        overflow: hidden;
      }
      .stat-card::before {
        content: '';
        position: absolute;
        inset-block: 0;
        inset-inline-start: 0;
        width: 4px;
        background: var(--subito-teal);
      }
      .stat-card:hover {
        transform: translateY(-2px);
        box-shadow: var(--subito-shadow);
        border-color: #b7ebe4;
      }
      .stat-label {
        color: var(--subito-muted);
        font-weight: 700;
        font-size: 0.9rem;
      }
      .stat-value {
        font-family: var(--subito-display);
        font-size: 1.85rem;
        letter-spacing: -0.03em;
        color: var(--subito-navy);
      }
    `,
  ],
})
export class ProviderHomeComponent implements OnInit {
  readonly tokens = inject(TokenStoreService);
  private readonly catalog = inject(CatalogService);
  private readonly orders = inject(OrdersService);
  readonly loading = signal(true);
  readonly counts = signal({ products: 0, categories: 0, orders: 0 });

  ngOnInit(): void {
    const query = { pageNumber: 1, pageSize: 1 };
    forkJoin({
      products: this.tokens.hasPermission('provider', 'ProviderProduct.Read') ? this.catalog.listProviderProducts(query) : of(null),
      categories: this.tokens.hasPermission('provider', 'ProviderCategory.Read') ? this.catalog.listProviderCategories(query) : of(null),
      orders: this.tokens.hasPermission('provider', 'ProviderOrder.Read') ? this.orders.listProvider(query) : of(null),
    }).subscribe({
      next: (res) => {
        this.counts.set({
          products: readPage(res.products).totalCount,
          categories: readPage(res.categories).totalCount,
          orders: readPage(res.orders).totalCount,
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
