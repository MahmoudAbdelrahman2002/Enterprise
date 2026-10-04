import { TooltipDirective } from '../../../shared/directives/tooltip.directive';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { Component, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ClientProductDto } from '../../../core/models/domain.models';
import { CartService } from '../../../core/services/cart.service';
import { CatalogService } from '../../../core/services/catalog.service';
import { I18nService } from '../../../core/services/i18n.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { ToastService } from '../../../core/services/toast.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { BasketQuantityComponent } from '../../../shared/components/basket-quantity/basket-quantity.component';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [BasketQuantityComponent, TooltipDirective, IconComponent, MoneyPipe, RouterLink, TranslatePipe],
  template: `
    @if (loading()) {
      <div class="pdp card">
        <div class="thumb-lg skeleton"></div>
        <div class="skeleton line"></div>
        <div class="skeleton line short"></div>
      </div>
    } @else if (failed()) {
      <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost" type="button" (click)="reload()">{{ 'actions.retry' | t }}</button></div>
    } @else if (product()) {
      <div class="pdp card">
        <div class="pdp-media">
          @if (product()!.imageUrl) {
            <img [src]="product()!.imageUrl!" [alt]="product()!.name" />
          } @else {
            <div class="media-fallback fill">{{ product()!.name.slice(0, 1) }}</div>
          }
        </div>
        <div class="pdp-body stack">
          <a class="back muted" [routerLink]="['/stores', product()!.providerId]"><app-icon class="directional" name="left" /> {{ 'actions.back' | t }}</a>
          <h1 class="page-title">{{ product()!.name }}</h1>
          @if (product()!.description) {
            <p class="desc">{{ product()!.description }}</p>
          }
          <p class="price big">{{ product()!.price | money }}</p>
          <app-basket-quantity [providerId]="product()!.providerId" [productId]="product()!.id" />

          <div class="qty row">
            <span class="muted">{{ 'cart.items' | t }}</span>
            <div class="stepper" role="group" [attr.aria-label]="'cart.items' | t">
              <button class="btn btn-ghost" type="button" (click)="qty.set(Math.max(1, qty() - 1))" [disabled]="qty() <= 1 || busy()" appTooltip [attr.aria-label]="'cart.decrease' | t"><app-icon name="minus" />
              </button>
              <span class="qty-val">{{ qty() }}</span>
              <button class="btn btn-ghost" type="button" (click)="qty.set(qty() + 1)" [disabled]="busy()" appTooltip [attr.aria-label]="'cart.increase' | t"><app-icon name="plus" /></button>
            </div>
          </div>

          <div class="actions">
            <button class="btn btn-accent" type="button" [disabled]="busy()" (click)="add()">
              <app-icon name="basket" />{{ busy() ? ('loading' | t) : ('actions.addToCart' | t) }}
            </button>
            <a class="btn btn-ghost" [routerLink]="['/cart', product()!.providerId]"><app-icon name="basket" />{{ 'nav.cart' | t }}</a>
          </div>
        </div>
      </div>
    }
  `,
  styles: [
    `
      .pdp {
        display: grid;
        grid-template-columns: minmax(0, 1.1fr) minmax(0, 1fr);
        gap: 1.5rem;
        padding: 1rem;
      }
      .pdp-media {
        border-radius: 18px;
        overflow: hidden;
        background: var(--subito-teal-soft);
        min-height: 240px;
      }
      .pdp-media img,
      .fill {
        width: 100%;
        height: 100%;
        min-height: 280px;
        object-fit: contain; padding: 1.5rem;
      }
      .fill {
        display: grid;
        place-items: center;
        font-size: 3rem;
        font-weight: 800;
        color: var(--subito-navy);
      }
      .back {
        font-weight: 700;
        width: fit-content;
      }
      .desc {
        margin: 0;
        color: var(--subito-muted);
        line-height: 1.55;
      }
      .price.big {
        font-size: 1.6rem;
      }
      .stepper {
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
        border: 1px solid var(--subito-border);
        border-radius: 999px;
        padding: 0.15rem;
        background: #fff;
      }
      .stepper .btn {
        min-height: 44px;
        min-width: 44px;
        padding: 0;
        border: 0;
      }
      .qty-val {
        min-width: 1.75rem;
        text-align: center;
        font-weight: 800;
      }
      .actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.75rem;
      }
      .actions .btn-accent {
        flex: 1 1 180px;
      }
      .line {
        height: 14px;
        margin-top: 1rem;
      }
      .line.short {
        width: 40%;
      }
      @media (max-width: 800px) {
        .pdp {
          grid-template-columns: 1fr;
        }
        .actions {
          position: static;
          background: linear-gradient(transparent, #fff 30%);
          padding-top: 1rem;
        }
      }
    `,
  ],
})
export class ProductDetailComponent {
  private readonly catalog = inject(CatalogService);
  private readonly cartApi = inject(CartService);
  private readonly route = inject(ActivatedRoute);
  private readonly tokens = inject(TokenStoreService);
  private readonly toast = inject(ToastService);
  private readonly i18n = inject(I18nService);
  readonly product = signal<ClientProductDto | null>(null);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly qty = signal(1);
  readonly busy = signal(false);
  readonly Math = Math;
  private productId = '';

  constructor() {
    this.productId = this.route.snapshot.paramMap.get('productId') || '';
    effect(() => {
      this.i18n.lang();
      this.reload();
    });
  }

  reload(): void {
    if (!this.productId) return;
    this.loading.set(true); this.failed.set(false);
    this.catalog.getClientProduct(this.productId).subscribe({
      next: (p) => {
        this.product.set(p);
        this.loading.set(false);
      },
      error: () => { this.failed.set(true); this.loading.set(false); },
    });
  }

  add(): void {
    const p = this.product();
    if (!p) return;
    if (!this.tokens.isAuthenticated('client')) {
      this.toast.info(this.i18n.t('auth.signInToShop'));
      return;
    }
    if (this.busy()) return;
    this.busy.set(true);
    this.cartApi.addItem(p.providerId, p.id, this.qty()).subscribe({
      next: () => {
        this.toast.success(this.i18n.t('cart.added'));
        this.busy.set(false);
      },
      error: () => this.busy.set(false),
    });
  }
}
