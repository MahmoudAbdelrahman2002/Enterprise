import { Component, OnInit, DestroyRef, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { OrderDetailDto } from '../../../core/models/domain.models';
import { OrdersService } from '../../../core/services/orders.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { CartService } from '../../../core/services/cart.service';

@Component({
  selector: 'app-payment-success',
  standalone: true,
  imports: [RouterLink, TranslatePipe, IconComponent],
  template: `
    <div class="card stack state-card" aria-live="polite" [attr.aria-busy]="!order() && !failed()">
      <div class="state-icon"><app-icon [name]="order() ? 'check' : 'receipt'" /></div>
      <h1 class="page-title">{{ (order() ? 'payment.success' : failed() ? 'payment.unconfirmed' : 'payment.waitingOrder') | t }}</h1>
      @if (failed()) {
        <p class="muted">{{ 'payment.retryHint' | t }}</p>
        @if (sessionId) { <button class="btn btn-primary" type="button" (click)="retry()">{{ 'actions.retry' | t }}</button> }
        <a class="btn btn-ghost" routerLink="/orders">{{ 'nav.orders' | t }}</a>
      } @else if (!order()) { <p class="muted">{{ 'payment.waitingHint' | t }}</p> }
      @else {
        <p>{{ 'payment.confirmed' | t }}</p>
        <a class="btn btn-primary" [routerLink]="['/orders', order()!.id]">{{ 'order.view' | t }}</a>
      }
    </div>
  `,
})
export class PaymentSuccessComponent implements OnInit {
  private readonly orders = inject(OrdersService);
  private readonly route = inject(ActivatedRoute);
  private readonly cart = inject(CartService);
  private readonly destroy = inject(DestroyRef);
  readonly order = signal<OrderDetailDto | null>(null);
  readonly failed = signal(false);
  sessionId = '';
  private timer?: ReturnType<typeof setTimeout>;
  private attempts = 0;
  private disposed = false;

  constructor() { this.destroy.onDestroy(() => { this.disposed = true; clearTimeout(this.timer); }); }

  ngOnInit(): void {
    this.sessionId = this.route.snapshot.queryParamMap.get('session_id') || '';
    if (!this.sessionId) { this.failed.set(true); return; }
    this.retry();
  }

  retry(): void { this.attempts = 0; this.failed.set(false); this.confirm(this.sessionId); }
  private confirmed(o: OrderDetailDto): void {
    if (this.disposed) return;
    if (!o?.id) { this.poll(this.sessionId); return; }
    this.order.set(o); this.cart.refreshCount();
  }

  private confirm(sessionId: string): void {
    this.orders.confirmSession(sessionId).subscribe({
      next: (o) => this.confirmed(o),
      error: () => this.poll(sessionId),
    });
  }

  private poll(sessionId: string): void {
    if (this.disposed) return;
    this.orders.getBySession(sessionId).subscribe({
      next: (o) => {
        if (o?.id) this.confirmed(o);
        else this.schedulePoll(sessionId);
      },
      error: () => {
        this.schedulePoll(sessionId);
      },
    });
  }
  private schedulePoll(sessionId: string): void {
    if (this.disposed) return;
    if (this.attempts++ < 8) this.timer = setTimeout(() => this.poll(sessionId), 1500);
    else this.failed.set(true);
  }
}
