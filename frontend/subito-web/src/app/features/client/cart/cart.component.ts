import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { VALIDATION_POLICY as P } from '../../../shared/forms/validation-policy';
import { ConfirmService } from '../../../core/services/confirm.service';
import { TooltipDirective } from '../../../shared/directives/tooltip.directive';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ShoppingCartDto } from '../../../core/models/domain.models';
import { CartService } from '../../../core/services/cart.service';
import { ToastService } from '../../../core/services/toast.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { CartCountPipe } from '../../../shared/pipes/cart-count.pipe';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [PaginationComponent, CartCountPipe, TooltipDirective, IconComponent, MoneyPipe, RouterLink, EmptyStateComponent, TranslatePipe],
  template: `
    <section class="cart-hero card">
      <div class="cart-hero__top">
        <div class="cart-hero__titles">
          <p class="cart-hero__eyebrow">Subito</p>
          <h1 class="page-title cart-hero__title">{{ 'nav.cart' | t }}</h1>
          @if (cart()?.providerName) {
            <p class="cart-hero__store">
              <span class="cart-hero__dot" aria-hidden="true"></span>
              {{ cart()!.providerName }}
            </p>
          }
        </div>
        <div class="cart-hero__actions">
          @if (cart()?.items?.length) { <button class="btn btn-ghost" type="button" [disabled]="busy() || updatingId() !== null" (click)="clearBasket()"><app-icon name="trash" />{{ 'cart.clear' | t }}</button> }
          <a class="btn btn-ghost" routerLink="/cart">{{ 'cart.allCarts' | t }}</a>
          <a class="btn btn-ghost" [routerLink]="['/stores', providerId]">{{ 'actions.back' | t }}</a>
        </div>
      </div>
    </section>

    @if (loading()) {
      <div class="cart-loading" aria-live="polite">
        <div class="cart-loading__block"></div>
        <div class="cart-loading__block cart-loading__block--side"></div>
        <p class="muted">{{ 'loading' | t }}</p>
      </div>
    } @else if (failed()) {
      <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost" type="button" (click)="reload()">{{ 'actions.retry' | t }}</button></div>
    } @else if (!cart() || !cart()!.items.length) {
      <div class="cart-empty card">
        <div class="cart-empty__icon" aria-hidden="true">
          <app-icon name="basket" />
        </div>
        <app-empty-state messageKey="empty.cart" />
        <a class="btn btn-primary" [routerLink]="['/stores', providerId]">{{ 'cart.continueShopping' | t }}</a>
      </div>
    } @else {
      <div class="cart-layout">
        <div class="cart-items"><ul class="cart-list card" role="list">
          @for (item of visibleItems(); track item.id; let last = $last) {
            <li class="cart-row" [class.is-busy]="updatingId() === item.id" [class.is-last]="last">
              <div class="cart-row__media">
                @if (item.productImage) {
                  <img class="thumb cart-row__thumb" [src]="item.productImage" [alt]="item.productName" />
                } @else {
                  <div class="thumb cart-row__thumb cart-row__thumb--placeholder" aria-hidden="true"></div>
                }
              </div>

              <div class="cart-row__body">
                <div class="cart-row__info">
                  <strong class="cart-row__name">{{ item.productName }}</strong>
                  <span class="muted cart-row__unit"><span class="sr-only">{{ 'cart.unitPrice' | t }} </span>{{ item.price | money }}</span>
                </div>

                <div class="cart-row__actions">
                  <div class="qty" [attr.aria-busy]="updatingId() === item.id">
                    <button
                      type="button"
                      class="qty__btn"
                      [disabled]="updatingId() !== null || busy() || item.quantity <= 1"
                      (click)="changeQty(item.id, item.quantity, item.quantity - 1)"
                      appTooltip [attr.aria-label]="'cart.decrease' | t"
                    ><app-icon name="minus" />
                    </button>
                    <span class="qty__value">{{ item.quantity }}</span>
                    <button
                      type="button"
                      class="qty__btn"
                      [disabled]="updatingId() !== null || busy() || item.quantity >= maxQuantity"
                      (click)="changeQty(item.id, item.quantity, item.quantity + 1)"
                      appTooltip [attr.aria-label]="'cart.increase' | t"
                    ><app-icon name="plus" />
                    </button>
                  </div>

                  <div class="cart-row__meta">
                    <span class="cart-row__line">
                      <span class="muted cart-row__line-label">{{ 'cart.lineTotal' | t }}</span>
                      <strong>{{ item.price * item.quantity | money }}</strong>
                    </span>
                    <button
                      class="cart-row__remove"
                      type="button"
                      [disabled]="updatingId() !== null || busy()"
                      (click)="remove(item.id)"
                    >
                      <app-icon name="trash" />{{ 'actions.delete' | t }}
                    </button>
                  </div>
                </div>
              </div>
            </li>
          }
        </ul>
        <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="cart()!.items.length" [disabled]="busy() || updatingId() !== null" labelKey="pagination.basketItems" (change)="page = $event" />
        </div>

        <aside class="cart-summary card">
          <div class="cart-summary__accent" aria-hidden="true"></div>
          <p class="notice" style="margin:1rem">{{ 'cart.separateCheckout' | t }}</p><h2 class="cart-summary__title">{{ 'cart.summary' | t }}</h2>
          <div class="cart-summary__rows">
            <div class="cart-summary__row">
              <span class="muted">{{ cart()!.items.length | cartCount:'product' }}</span>
              <span class="badge">{{ itemQuantity() | cartCount }}</span>
            </div>
            <div class="cart-summary__row cart-summary__total">
              <span>{{ 'cart.subtotal' | t }}</span>
              <strong class="cart-summary__price">{{ cart()!.totalPrice | money }}</strong>
            </div>
          </div>
          <button
            class="btn btn-primary cart-summary__checkout"
            type="button"
            [disabled]="busy() || updatingId() !== null"
            (click)="checkout()"
          >
            <app-icon name="basket" />{{ (busy() ? 'cart.checkingOut' : 'actions.checkout') | t }}
          </button>
          @if (checkoutFailed()) { <p class="field-error" role="alert" style="margin:1rem">{{ 'cart.checkoutFailed' | t }}</p> }
          <a class="cart-summary__continue" [routerLink]="['/stores', providerId]">
            {{ 'cart.continueShopping' | t }}
          </a>
        </aside>
      </div>
    }
  `,
  styles: [
    `
      :host {
        display: block;
        animation: cart-fade 0.35s ease both;
      }

      @keyframes cart-fade {
        from {
          opacity: 0;
          transform: translateY(6px);
        }
        to {
          opacity: 1;
          transform: none;
        }
      }

      .cart-hero {
        margin-block-end: 1.25rem;
        padding: 1.35rem 1.4rem;
        background:
          radial-gradient(120% 90% at 100% 0%, rgba(0, 199, 177, 0.18), transparent 55%),
          linear-gradient(135deg, #ffffff 0%, var(--subito-teal-soft) 100%);
        overflow: hidden;
      }

      .cart-hero__top {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: 1rem;
      }

      .cart-hero__actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
        flex-shrink: 0;
      }

      .cart-hero__titles {
        min-width: 0;
        text-align: start;
      }

      .cart-hero__eyebrow {
        margin: 0 0 0.35rem;
        font-size: 0.75rem;
        font-weight: 700;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        color: var(--subito-navy);
      }

      .cart-hero__title {
        margin: 0;
        font-size: clamp(1.6rem, 3vw, 2rem);
      }

      .cart-hero__store {
        display: inline-flex;
        align-items: center;
        gap: 0.45rem;
        margin: 0.55rem 0 0;
        padding: 0.35rem 0.75rem;
        border-radius: 999px;
        background: rgba(255, 255, 255, 0.72);
        border: 1px solid var(--subito-border);
        color: var(--subito-navy);
        font-size: 0.9rem;
      }

      .cart-hero__dot {
        width: 0.45rem;
        height: 0.45rem;
        border-radius: 999px;
        background: var(--subito-navy);
        flex-shrink: 0;
      }

      .cart-loading {
        display: grid;
        grid-template-columns: minmax(0, 1fr) minmax(220px, 280px);
        gap: 1.25rem;
        align-items: start;
      }

      .cart-loading__block {
        min-height: 180px;
        border-radius: var(--subito-radius);
        border: 1px solid var(--subito-border);
        background:
          linear-gradient(90deg, var(--subito-teal-soft) 25%, #fff 50%, var(--subito-teal-soft) 75%);
        background-size: 200% 100%;
        animation: cart-shimmer 1.2s ease infinite;
      }

      .cart-loading__block--side {
        min-height: 220px;
      }

      .cart-loading .muted {
        grid-column: 1 / -1;
        margin: 0;
      }

      @keyframes cart-shimmer {
        from {
          background-position: 100% 0;
        }
        to {
          background-position: -100% 0;
        }
      }

      .cart-empty {
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

      .cart-empty__icon {
        display: grid;
        place-items: center;
        width: 4.5rem;
        height: 4.5rem;
        border-radius: 1.25rem;
        color: var(--subito-navy);
        background: color-mix(in srgb, var(--subito-navy) 10%, var(--subito-teal-soft));
      }

      .cart-empty ::ng-deep .card {
        border: 0;
        box-shadow: none;
        background: transparent;
        padding: 0;
      }

      .cart-layout {
        display: grid;
        grid-template-columns: minmax(0, 1fr) minmax(260px, 320px);
        gap: 1.25rem;
        align-items: start;
      }

      .cart-items { min-width: 0; }
      .cart-list {
        list-style: none;
        margin: 0;
        padding: 0.35rem 0.25rem;
        display: flex;
        flex-direction: column;
        overflow: hidden;
      }

      .cart-row {
        display: flex;
        gap: 1rem;
        padding: 1.1rem 1rem;
        align-items: center;
        border-block-end: 1px solid var(--subito-border);
        transition: background 0.15s ease, opacity 0.15s ease;
      }

      .cart-row.is-last {
        border-block-end: 0;
      }

      .cart-row:hover {
        background: color-mix(in srgb, var(--subito-teal-soft) 55%, transparent);
      }

      .cart-row.is-busy {
        opacity: 0.65;
        pointer-events: none;
      }

      .cart-row__media {
        flex-shrink: 0;
      }

      .cart-row__thumb {
        width: 72px;
        height: 72px;
        object-fit: contain;
        border-radius: calc(var(--subito-radius) - 2px);
        display: block;
        box-shadow: 0 4px 14px rgba(16, 36, 58, 0.08);
      }

      .cart-row__thumb--placeholder {
        background:
          linear-gradient(145deg, var(--subito-teal-soft), var(--subito-teal-soft));
        border: 1px solid var(--subito-border);
        box-shadow: none;
      }

      .cart-row__body {
        flex: 1;
        min-width: 0;
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        justify-content: space-between;
        gap: 0.85rem 1.25rem;
      }

      .cart-row__info {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
        min-width: 0;
        text-align: start;
        flex: 1 1 140px;
      }

      .cart-row__name {
        font-family: var(--subito-display);
        font-size: 1.05rem;
        color: var(--subito-navy);
        overflow-wrap: anywhere;
      }

      .cart-row__unit {
        font-size: 0.9rem;
      }

      .cart-row__actions {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 0.85rem 1.1rem;
        margin-inline-start: auto;
      }

      .cart-row__meta {
        display: flex;
        flex-direction: column;
        align-items: flex-end;
        gap: 0.35rem;
        min-width: 6.5rem;
        text-align: end;
      }

      .cart-row__line {
        display: flex;
        flex-direction: column;
        gap: 0.1rem;
      }

      .cart-row__line-label {
        font-size: 0.72rem;
        text-transform: uppercase;
        letter-spacing: 0.04em;
      }

      .cart-row__remove {
        border: 0;
        background: transparent;
        padding: .5rem; min-height: 44px; display: inline-flex; align-items: center; gap: .4rem;
        color: var(--subito-danger);
        font: inherit;
        font-size: 0.85rem;
        font-weight: 600;
        cursor: pointer;
        text-decoration: underline;
        text-underline-offset: 0.18em;
      }

      .cart-row__remove:hover:not(:disabled) {
        color: var(--subito-danger);
      }

      .cart-row__remove:disabled {
        opacity: 0.45;
        cursor: not-allowed;
      }

      .qty {
        display: inline-flex;
        align-items: center;
        gap: 0.1rem;
        padding: 0.2rem;
        border: 1px solid var(--subito-border);
        border-radius: 999px;
        background: linear-gradient(180deg, #ffffff 0%, var(--subito-page) 100%);
        box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.9);
      }

      .qty__btn {
        width: 44px;
        height: 44px;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        border: 0;
        border-radius: 999px;
        background: var(--subito-teal-soft);
        color: var(--subito-navy);
        font: inherit;
        font-size: 1.15rem;
        line-height: 1;
        cursor: pointer;
        transition: background 0.15s ease, color 0.15s ease, transform 0.12s ease;
      }

      .qty__btn:hover:not(:disabled) {
        background: color-mix(in srgb, var(--subito-navy) 14%, var(--subito-teal-soft));
        color: var(--subito-navy);
      }

      .qty__btn:active:not(:disabled) {
        transform: scale(0.94);
      }

      .qty__btn:disabled {
        opacity: 0.4;
        cursor: not-allowed;
      }

      .qty__value {
        min-width: 1.85rem;
        text-align: center;
        font-weight: 700;
        color: var(--subito-navy);
      }

      .cart-summary {
        position: sticky;
        top: 108px;
        padding: 0;
        overflow: hidden;
        display: flex;
        flex-direction: column;
        background:
          linear-gradient(180deg, #ffffff 0%, var(--subito-page) 100%);
      }

      .cart-summary__accent {
        height: 4px;
        background: linear-gradient(90deg, var(--subito-navy), var(--subito-warm));
      }

      .cart-summary__title {
        margin: 0;
        padding: 1.15rem 1.25rem 0.35rem;
        font-family: var(--subito-display);
        font-style: normal;
        font-size: 1.35rem;
        color: var(--subito-navy);
        text-align: start;
      }

      .cart-summary__rows {
        display: flex;
        flex-direction: column;
        gap: 0.85rem;
        padding: 0.75rem 1.25rem 1rem;
      }

      .cart-summary__row {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 1rem;
        text-align: start;
      }

      .cart-summary__total {
        padding-block-start: 0.85rem;
        border-block-start: 1px dashed var(--subito-border);
      }

      .cart-summary__price {
        font-family: var(--subito-display);
        font-size: 1.45rem;
        color: var(--subito-navy);
      }

      .cart-summary__checkout {
        width: calc(100% - 2.5rem);
        margin: 0 1.25rem;
        justify-content: center;
        min-height: 2.85rem;
        box-shadow: 0 8px 18px rgba(0, 199, 177, 0.22);
      }

      .cart-summary__checkout:hover:not(:disabled) {
        transform: translateY(-1px);
      }

      .cart-summary__continue {
        display: block;
        margin: 0.9rem 1.25rem 1.25rem;
        text-align: center;
        color: var(--subito-muted);
        font-size: 0.9rem;
        font-weight: 600;
        text-decoration: underline;
        text-underline-offset: 0.18em;
      }

      .cart-summary__continue:hover {
        color: var(--subito-navy);
      }

      @media (max-width: 767px) {
        .cart-layout,
        .cart-loading {
          grid-template-columns: 1fr;
        }

        .cart-summary {
          position: static;
          order: 2;
        }

        .cart-list {
          order: 1;
        }

        .cart-row {
          align-items: flex-start;
        }

        .cart-row__actions {
          margin-inline-start: 0;
          width: 100%;
          justify-content: space-between;
        }

        .cart-row__meta {
          align-items: flex-end;
        }

        .cart-hero__top {
          flex-direction: column;
        }
      }
    `,
  ],
})
export class CartComponent implements OnInit {
  private readonly confirm = inject(ConfirmService);
  private readonly cartApi = inject(CartService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  providerId = '';
  page = 1;
  readonly pageSize = 10;
  get totalPages(): number { return Math.max(1, Math.ceil((this.cart()?.items.length ?? 0) / this.pageSize)); }
  visibleItems() { return (this.cart()?.items ?? []).slice((this.page - 1) * this.pageSize, this.page * this.pageSize); }
  readonly cart = signal<ShoppingCartDto | null>(null);
  readonly busy = signal(false);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly updatingId = signal<string | null>(null);
  readonly checkoutFailed = signal(false);

  ngOnInit(): void {
    this.providerId = this.route.snapshot.paramMap.get('providerId') || '';
    this.reload();
  }

  reload(): void {
    this.loading.set(true); this.failed.set(false);
    this.cartApi.get(this.providerId).subscribe({
      next: (c) => {
        this.cart.set(c);
        this.page = Math.min(this.page, this.totalPages);
        this.loading.set(false);
        this.updatingId.set(null);
      },
      error: () => {
        this.failed.set(true);
        this.loading.set(false);
        this.updatingId.set(null);
      },
    });
  }

  readonly maxQuantity = P.QuantityMax;
  changeQty(id: string, current: number, quantity: number): void {
    if (!Number.isSafeInteger(quantity) || quantity < 1 || quantity > P.QuantityMax || quantity === current) return;
    this.update(id, quantity);
  }

  update(id: string, quantity: number): void {
    const qty = Number(quantity);
    if (!Number.isSafeInteger(qty) || qty < 1 || qty > P.QuantityMax || this.busy() || this.updatingId()) return;
    this.updatingId.set(id);
    this.cartApi.updateItem(this.providerId, id, qty).subscribe({
      next: () => this.reload(),
      error: () => this.updatingId.set(null),
    });
  }

  remove(id: string): void {
    if (this.busy() || this.updatingId()) return;
    this.updatingId.set(id);
    this.cartApi.removeItem(this.providerId, id).subscribe({ next: () => this.reload(), error: () => this.updatingId.set(null) });
  }
  itemQuantity(): number { return this.cart()?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0; }
  async clearBasket(): Promise<void> {
    if (this.busy() || this.updatingId()) return;
    if (!(await this.confirm.ask('cart.confirmClear'))) return;
    this.updatingId.set('clear');
    this.cartApi.clear(this.providerId).subscribe({
      next: () => { this.cart.set(null); this.updatingId.set(null); },
      error: () => this.updatingId.set(null),
    });
  }

  checkout(): void {
    if (this.busy() || this.updatingId()) return;
    this.busy.set(true);
    this.checkoutFailed.set(false);
    this.cartApi.checkout(this.providerId).subscribe({
      next: (session) => {
        window.location.href = session.url;
      },
      error: () => { this.checkoutFailed.set(true); this.busy.set(false); },
    });
  }
}
