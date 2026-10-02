import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { IconComponent } from '../../../shared/components/icon/icon.component';

@Component({
  selector: 'app-payment-cancel',
  standalone: true,
  imports: [RouterLink, TranslatePipe, IconComponent],
  template: `
    <div class="card stack state-card">
      <div class="state-icon"><app-icon name="basket" /></div>
      <h1 class="page-title">{{ 'payment.cancel' | t }}</h1>
      <p class="muted">{{ 'payment.cartUnchanged' | t }}</p>
      <a class="btn btn-primary" routerLink="/cart">{{ 'cart.allCarts' | t }}</a>
      <a class="btn btn-ghost" routerLink="/">{{ 'cart.continueShopping' | t }}</a>
    </div>
  `,
})
export class PaymentCancelComponent {}
