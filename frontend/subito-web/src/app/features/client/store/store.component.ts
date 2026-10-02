import { IconComponent } from '../../../shared/components/icon/icon.component';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { Component, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import {
  ClientCategoryDto,
  ClientProductDto,
  ClientProviderListItemDto,
} from '../../../core/models/domain.models';
import { CartService } from '../../../core/services/cart.service';
import { CatalogService } from '../../../core/services/catalog.service';
import { I18nService } from '../../../core/services/i18n.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { ToastService } from '../../../core/services/toast.service';
import { readList } from '../../../core/utils/read-list';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { SearchFieldComponent } from '../../../shared/components/search-field/search-field.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-store',
  standalone: true,
  imports: [IconComponent, MoneyPipe, RouterLink, ReactiveFormsModule, TranslatePipe, EmptyStateComponent, SearchFieldComponent],
  template: `
    @if (store()) {
      <section class="store-hero card">
        <div class="store-hero-main">
          @if (store()!.imageUrl) {
            <img class="store-cover" [src]="store()!.imageUrl!" [alt]="store()!.companyName" />
          } @else {
            <div class="store-cover media-fallback">{{ store()!.companyName.slice(0, 1) }}</div>
          }
          <div>
            <p class="muted eyebrow">{{ store()!.serviceName }}</p>
            <h1 class="page-title">{{ store()!.companyName }}</h1>
            @if (store()!.phoneNumber) {
              <p class="muted">{{ store()!.phoneNumber }}</p>
            }
          </div>
        </div>
        <div class="store-hero-actions">
          <app-search-field class="store-search" [control]="search" placeholderKey="search.products" />
          <a class="btn btn-accent" [routerLink]="['/cart', providerId]"><app-icon name="basket" />{{ 'nav.cart' | t }}</a>
        </div>
      </section>
    }

    <div class="chip-scroll" role="group" [attr.aria-label]="'nav.categories' | t">
      <button class="chip" type="button" [class.active]="!categoryId" [attr.aria-pressed]="!categoryId" (click)="selectCategory(null)">
        {{ 'actions.all' | t }}
      </button>
      @for (c of categories(); track c.id) {
        <button class="chip" type="button" [class.active]="categoryId === c.id" [attr.aria-pressed]="categoryId === c.id" (click)="selectCategory(c.id)">
          {{ c.name }}
        </button>
      }
    </div>

    @if (loading()) {
      <div class="grid-cards" style="margin-top:1rem">
        @for (_ of [1, 2, 3, 4, 5, 6]; track $index) {
          <div class="card product-card">
            <div class="thumb-lg skeleton"></div>
            <div class="skeleton line"></div>
            <div class="skeleton line short"></div>
          </div>
        }
      </div>
    } @else if (failed()) {
      <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost" type="button" (click)="loadProducts()">{{ 'actions.retry' | t }}</button></div>
    } @else if (!products().length) {
      <app-empty-state />
    } @else {
      <div class="grid-cards" style="margin-top:1rem">
        @for (p of products(); track p.id) {
          <article class="card product-card">
            <a [routerLink]="['/products', p.id]" class="product-link">
              @if (p.imageUrl) {
                <img class="thumb-lg" [src]="p.imageUrl" [alt]="p.name" loading="lazy" />
              } @else {
                <div class="thumb-lg media-fallback">{{ p.name.slice(0, 1) }}</div>
              }
              <h3>{{ p.name }}</h3>
            </a>
            @if (p.description) {
              <p class="muted clamp">{{ p.description }}</p>
            }
            <div class="product-foot">
              <span class="price">{{ p.price | money }}</span>
              <button class="btn btn-primary" type="button" [disabled]="addingId() !== null" (click)="add(p)">
                <app-icon name="basket" />{{ addingId() === p.id ? ('loading' | t) : ('actions.addToCart' | t) }}
              </button>
            </div>
          </article>
        }
      </div>
    }
  `,
  styles: [
    `
      .store-hero {
        margin-bottom: 1rem;
        display: grid;
        gap: 1rem;
      }
      .store-hero-main {
        display: flex;
        gap: 1rem;
        align-items: center;
      }
      .store-cover {
        width: 88px;
        height: 88px;
        border-radius: 18px;
        object-fit: contain;
        flex: 0 0 auto;
        background: var(--subito-teal-soft);
      }
      .eyebrow {
        margin: 0 0 0.25rem;
        font-size: 0.8rem;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.04em;
      }
      .page-title {
        margin-bottom: 0.25rem;
      }
      .store-hero-actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.75rem;
        align-items: center;
      }
      .store-search {
        flex: 1 1 220px;
        width: min(100%, 420px);
      }
      .product-card {
        display: flex;
        flex-direction: column;
        gap: 0.55rem;
        padding: 0.85rem;
      }
      .product-link h3 {
        margin: 0.35rem 0 0;
        font-family: var(--subito-display);
        font-size: 1rem;
        letter-spacing: -0.02em;
      }
      .clamp {
        margin: 0;
        display: -webkit-box;
        -webkit-line-clamp: 2;
        -webkit-box-orient: vertical;
        overflow: hidden;
        font-size: 0.9rem;
      }
      .product-card img { object-fit: contain; padding: 1rem; background: var(--subito-page); aspect-ratio: 1; }
      .product-foot {
        display: flex;
        align-items: stretch; flex-direction: column; gap: .75rem; margin-top: auto;
      }
      .product-foot .btn {
        padding-inline: 0.95rem;
      }
      .line {
        height: 12px;
        margin-top: 0.65rem;
      }
      .line.short {
        width: 40%;
      }
      @media (max-width: 560px) {
        .store-hero-actions .btn {
          width: 100%;
        }
      }
    `,
  ],
})
export class StoreComponent {
  private readonly catalog = inject(CatalogService);
  private readonly cartApi = inject(CartService);
  private readonly route = inject(ActivatedRoute);
  private readonly tokens = inject(TokenStoreService);
  private readonly toast = inject(ToastService);
  private readonly i18n = inject(I18nService);
  providerId = '';
  categoryId: string | null = null;
  readonly store = signal<ClientProviderListItemDto | null>(null);
  readonly categories = signal<ClientCategoryDto[]>([]);
  readonly products = signal<ClientProductDto[]>([]);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly addingId = signal<string | null>(null);
  readonly search = new FormControl('', { nonNullable: true });

  constructor() {
    this.providerId = this.route.snapshot.paramMap.get('providerId') || '';
    this.search.valueChanges.pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed()).subscribe(() => this.loadProducts());
    effect(() => {
      this.i18n.lang();
      this.reloadCatalog();
    });
  }

  selectCategory(id: string | null): void {
    this.categoryId = id;
    this.loadProducts();
  }

  private reloadCatalog(): void {
    if (!this.providerId) return;
    this.catalog.getClientProvider(this.providerId).subscribe({
      next: (s) => this.store.set(s),
    });
    this.catalog.listStoreCategories(this.providerId).subscribe({
      next: (c) => this.categories.set(readList<ClientCategoryDto>(c)),
    });
    this.loadProducts();
  }

  loadProducts(): void {
    if (!this.providerId) return;
    this.loading.set(true); this.failed.set(false);
    this.catalog
      .listStoreProducts(this.providerId, {
        categoryId: this.categoryId,
        searchTerm: this.search.value || null,
        pageSize: 100,
      })
      .subscribe({
        next: (p) => {
          this.products.set(readList<ClientProductDto>(p));
          this.loading.set(false);
        },
        error: () => { this.failed.set(true); this.loading.set(false); },
      });
  }

  add(p: ClientProductDto): void {
    if (!this.tokens.isAuthenticated('client')) {
      this.toast.info(this.i18n.t('auth.signInToShop'));
      return;
    }
    if (this.addingId()) return;
    this.addingId.set(p.id);
    this.cartApi.addItem(this.providerId, p.id, 1).subscribe({
      next: () => {
        this.toast.success(this.i18n.t('cart.added'));
        this.addingId.set(null);
      },
      error: () => this.addingId.set(null),
    });
  }
}
