import { Component, effect, inject, input, signal } from '@angular/core';
import { CartService } from '../../../core/services/cart.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { TranslatePipe } from '../../pipes/translate.pipe';
import { IconComponent } from '../icon/icon.component';

@Component({
  selector: 'app-basket-quantity',
  standalone: true,
  imports: [TranslatePipe, IconComponent],
  template: `
    @if (quantity() > 0) {
      <span class="basket-quantity" role="status" aria-atomic="true">
        <app-icon name="basket" />{{ 'cart.inBasket' | t }} <strong>{{ quantity() }}</strong>
      </span>
    }
  `,
  styles: [`.basket-quantity{display:inline-flex;align-items:center;gap:.4rem;flex-wrap:wrap;padding:.35rem .65rem;border-radius:999px;background:var(--subito-teal-soft);color:var(--subito-navy);font-size:.875rem;font-weight:600}`],
})
export class BasketQuantityComponent {
  readonly providerId = input.required<string>();
  readonly productId = input.required<string>();
  readonly quantity = signal(0);
  private readonly carts = inject(CartService);
  private readonly tokens = inject(TokenStoreService);

  constructor() {
    effect(onCleanup => {
      const providerId = this.providerId();
      const productId = this.productId();
      const authenticated = this.tokens.isAuthenticated('client');
      this.quantity.set(0);
      if (!authenticated || !providerId || !productId) return;
      const subscription = this.carts.watchBasket(providerId).subscribe(basket => {
        this.quantity.set(basket?.items?.filter(item => item.productId === productId)
          .reduce((total, item) => total + item.quantity, 0) ?? 0);
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }
}
