import { OrderItemsComponent } from '../../../shared/components/order-items/order-items.component';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ORDER_STATUS_LABELS } from '../../../core/models/api.models';
import { OrderDetailDto } from '../../../core/models/domain.models';
import { OrdersService } from '../../../core/services/orders.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [OrderItemsComponent, MoneyPipe, RouterLink, DatePipe, TranslatePipe],
  template: `
    @if (loading()) { <p class="muted" role="status">{{ 'loading' | t }}</p> }
    @else if (failed()) { <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost" (click)="ngOnInit()">{{ 'actions.retry' | t }}</button></div> }
    @else if (order()) {
      <div class="toolbar">
        <h1 class="page-title">{{ 'order.detail' | t }}</h1>
        <a routerLink="/orders" class="btn btn-ghost">{{ 'actions.back' | t }}</a>
      </div>
      <div class="card stack">
        <div class="row" style="justify-content:space-between">
          <span>{{ order()!.orderDateUtc | date:'medium' }}</span>
          <span class="badge">{{ (order()!.isHistorical ? 'order.historical' : labels[order()!.status]) | t }}</span>
        </div>
        <app-order-items [items]="order()!.items" />
        @if (order()!.isHistorical) { <p class="muted">{{ 'order.historicalNote' | t }}</p> }
        <strong>{{ 'ui.total' | t }}: {{ order()!.totalAmount | money }}</strong>
      </div>
    }
  `,
})
export class OrderDetailComponent implements OnInit {
  private readonly orders = inject(OrdersService);
  private readonly route = inject(ActivatedRoute);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly order = signal<OrderDetailDto | null>(null);
  readonly labels = ORDER_STATUS_LABELS;

  ngOnInit(): void {
    this.loading.set(true); this.failed.set(false);
    const id = this.route.snapshot.paramMap.get('orderId')!;
    this.orders.getClient(id).subscribe({ next: (o) => { this.order.set(o); this.loading.set(false); }, error: () => { this.failed.set(true); this.loading.set(false); } });
  }

}
