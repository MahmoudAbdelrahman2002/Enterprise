import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { PageRequest } from '../../../core/utils/page-request';
import { readPage, resolvePage } from '../../../core/utils/read-list';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ShoppingCartDto } from '../../../core/models/domain.models';
import { CartService } from '../../../core/services/cart.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { CartCountPipe } from '../../../shared/pipes/cart-count.pipe';

@Component({
  selector: 'app-carts-list',
  standalone: true,
  imports: [PaginationComponent, CartCountPipe, IconComponent, MoneyPipe, RouterLink, EmptyStateComponent, TranslatePipe],
  template: `
    <section class="carts-hero card">
      <p class="carts-hero__eyebrow">Subito</p>
      <h1 class="page-title carts-hero__title">{{ 'nav.cart' | t }}</h1>
      <p class="muted carts-hero__sub">{{ 'cart.browseSubtitle' | t }}</p>
    </section>

    @if (loading()) {
      <div class="carts-loading" aria-live="polite">
        <div class="carts-loading__card"></div>
        <div class="carts-loading__card"></div>
        <p class="muted">{{ 'loading' | t }}</p>
      </div>
    } @else if (failed()) {
      <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost" type="button" (click)="reload()">{{ 'actions.retry' | t }}</button></div>
    } @else if (!carts().length) {
      <div class="carts-empty card">
        <div class="carts-empty__icon" aria-hidden="true">
          <app-icon name="basket" />
        </div>
        <app-empty-state messageKey="empty.cart" />
        <a class="btn btn-primary" routerLink="/">{{ 'cart.continueShopping' | t }}</a>
      </div>
    } @else {
      <div class="carts-grid">
        @for (cart of carts(); track cart.id) {
          <a class="cart-card card" [routerLink]="['/cart', cart.providerId]">
            <div class="cart-card__top">
              <div class="cart-card__store">
                <span class="cart-card__dot" aria-hidden="true"></span>
                <strong>{{ cart.providerName || ('cart.store' | t) }}</strong>
              </div>
              <span class="badge">{{ cart.items.length | cartCount:'product' }} · {{ unitCount(cart) | cartCount }}</span>
            </div>

            <ul class="cart-card__preview" role="list">
              @for (item of previewItems(cart); track item.id) {
                <li class="cart-card__item">
                  @if (item.productImage) {
                    <img class="cart-card__thumb" [src]="item.productImage" [alt]="item.productName" />
                  } @else {
                    <div class="cart-card__thumb cart-card__thumb--placeholder" aria-hidden="true"></div>
                  }
                  <div class="cart-card__item-info">
                    <span class="cart-card__name">{{ item.productName }}</span>
                    <span class="muted">× {{ item.quantity }}</span>
                  </div>
                </li>
              }
              @if (cart.items.length > 3) {
                <li class="cart-card__more muted">+{{ cart.items.length - 3 }} {{ 'cart.moreItems' | t }}</li>
              }
            </ul>

            <div class="cart-card__footer">
              <div>
                <span class="muted cart-card__label">{{ 'cart.subtotal' | t }}</span>
                <strong class="cart-card__total">{{ cart.totalPrice | money }}</strong>
              </div>
              <span class="cart-card__cta">{{ 'cart.openCart' | t }} <app-icon class="directional" name="right" /></span>
            </div>
          </a>
        }
      </div>
    }
    <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" [disabled]="loading()" labelKey="pagination.baskets" (change)="reload($event)" />
  `,
  styles: [
    `
      :host {
        display: block;
        animation: carts-fade 0.35s ease both;
      }

      @keyframes carts-fade {
        from {
          opacity: 0;
          transform: translateY(6px);
        }
        to {
          opacity: 1;
          transform: none;
        }
      }

      .carts-hero {
        margin-block-end: 1.25rem;
        padding: 1.35rem 1.4rem;
        background:
          radial-gradient(120% 90% at 100% 0%, rgba(0, 199, 177, 0.18), transparent 55%),
          linear-gradient(135deg, #ffffff 0%, var(--subito-teal-soft) 100%);
      }

      .carts-hero__eyebrow {
        margin: 0 0 0.35rem;
        font-size: 0.75rem;
        font-weight: 700;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        color: var(--subito-navy);
      }

      .carts-hero__title {
        margin: 0;
        font-size: clamp(1.6rem, 3vw, 2rem);
      }

      .carts-hero__sub {
        margin: 0.45rem 0 0;
        max-width: 36rem;
      }

      .carts-loading {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
        gap: 1rem;
      }

      .carts-loading__card {
        min-height: 220px;
        border-radius: var(--subito-radius);
        border: 1px solid var(--subito-border);
        background: linear-gradient(90deg, var(--subito-teal-soft) 25%, #fff 50%, var(--subito-teal-soft) 75%);
        background-size: 200% 100%;
        animation: carts-shimmer 1.2s ease infinite;
      }

      @keyframes carts-shimmer {
        from {
          background-position: 100% 0;
        }
        to {
          background-position: -100% 0;
        }
      }

      .carts-empty {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 1rem;
        padding: 2.5rem 1.5rem;
        text-align: center;
        background:
          radial-gradient(80% 70% at 50% 0%, rgba(0, 199, 177, 0.08), transparent 60%),
          var(--subito-card);
      }

      .carts-empty__icon {
        display: grid;
        place-items: center;
        width: 4.5rem;
        height: 4.5rem;
        border-radius: 1.25rem;
        color: var(--subito-navy);
        background: color-mix(in srgb, var(--subito-navy) 10%, var(--subito-teal-soft));
      }

      .carts-empty ::ng-deep .card {
        border: 0;
        box-shadow: none;
        background: transparent;
        padding: 0;
      }

      .carts-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
        gap: 1.1rem;
      }

      .cart-card {
        display: flex;
        flex-direction: column;
        gap: 1rem;
        padding: 1.15rem 1.2rem;
        text-decoration: none;
        color: inherit;
        transition: transform 0.18s ease, box-shadow 0.18s ease, border-color 0.18s ease;
        background: linear-gradient(180deg, #ffffff 0%, var(--subito-page) 100%);
        border: 1px solid var(--subito-border);
      }

      .cart-card:hover {
        transform: translateY(-3px);
        border-color: color-mix(in srgb, var(--subito-navy) 35%, var(--subito-border));
        box-shadow: 0 12px 28px rgba(16, 36, 58, 0.1);
      }

      .cart-card__top {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.75rem;
      }

      .cart-card__store {
        display: inline-flex;
        align-items: center;
        gap: 0.45rem;
        min-width: 0;
      }

      .cart-card__store strong {
        font-family: var(--subito-display);
        font-size: 1.15rem;
        color: var(--subito-navy);
        overflow-wrap: anywhere;
      }

      .cart-card__dot {
        width: 0.45rem;
        height: 0.45rem;
        border-radius: 999px;
        background: var(--subito-navy);
        flex-shrink: 0;
      }

      .cart-card__preview {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: 0.65rem;
        flex: 1;
      }

      .cart-card__item {
        display: flex;
        align-items: center;
        gap: 0.7rem;
      }

      .cart-card__thumb {
        width: 44px;
        height: 44px;
        object-fit: contain;
        border-radius: 10px;
        flex-shrink: 0;
      }

      .cart-card__thumb--placeholder {
        background: linear-gradient(145deg, var(--subito-teal-soft), var(--subito-teal-soft));
        border: 1px solid var(--subito-border);
      }

      .cart-card__item-info {
        display: flex;
        flex-direction: column;
        gap: 0.1rem;
        min-width: 0;
      }

      .cart-card__name {
        font-weight: 600;
        color: var(--subito-navy);
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .cart-card__more {
        font-size: 0.85rem;
        padding-inline-start: 0.15rem;
      }

      .cart-card__footer {
        display: flex;
        align-items: flex-end;
        justify-content: space-between;
        gap: 1rem;
        padding-block-start: 0.85rem;
        border-block-start: 1px dashed var(--subito-border);
      }

      .cart-card__label {
        display: block;
        font-size: 0.72rem;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        margin-bottom: 0.15rem;
      }

      .cart-card__total {
        font-family: var(--subito-display);
        font-size: 1.35rem;
        color: var(--subito-navy);
      }

      .cart-card__cta {
        color: var(--subito-navy);
        font-weight: 700;
        font-size: 0.9rem;
        white-space: nowrap;
      }
    `,
  ],
})
export class CartsListComponent implements OnInit {
  private readonly cartApi = inject(CartService);
  readonly carts = signal<ShoppingCartDto[]>([]);
  readonly loading = signal(true);
  readonly failed = signal(false);

  page = 1; totalPages = 1; totalCount = 0;
  private readonly pageRequest = new PageRequest(inject(DestroyRef));
  ngOnInit(): void { this.reload(); }
  reload(page = this.page): void {
    this.page = page;
    this.loading.set(true); this.failed.set(false);
    this.pageRequest.run(this.cartApi.listPage({ pageNumber: page, pageSize: 12 }), {
      next: (items) => {
        const result = readPage<ShoppingCartDto>(items);
        const targetPage = resolvePage(page, result);
        if (page !== targetPage) { this.reload(targetPage); return; }
        this.carts.set(result.items); this.totalPages = result.totalPages; this.totalCount = result.totalCount;
        this.loading.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.loading.set(false);
      },
    });
  }

  previewItems(cart: ShoppingCartDto) {
    return cart.items.slice(0, 3);
  }

  unitCount(cart: ShoppingCartDto): number {
    return cart.items.reduce((total, item) => total + item.quantity, 0);
  }
}
