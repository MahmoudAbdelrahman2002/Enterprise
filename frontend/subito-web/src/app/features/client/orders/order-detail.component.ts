import { I18nService } from '../../../core/services/i18n.service';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ORDER_STATUS_LABELS, OrderStatus } from '../../../core/models/api.models';
import { OrderDetailDto } from '../../../core/models/domain.models';
import { OrdersService } from '../../../core/services/orders.service';
import { ToastService } from '../../../core/services/toast.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { ConfirmService } from '../../../core/services/confirm.service';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [MoneyPipe, RouterLink, DatePipe, TranslatePipe],
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
          <span class="badge">{{ labels[order()!.status] | t }}</span>
        </div>
        @for (i of order()!.items; track i.id) {
          <div class="row" style="justify-content:space-between">
            <span>{{ i.productName }} × {{ i.quantity }}</span>
            <span>{{ i.lineTotal | money }}</span>
          </div>
        }
        <strong>{{ 'ui.total' | t }}: {{ order()!.totalAmount | money }}</strong>
        @if (order()!.status === pending) {
          <button class="btn btn-danger" type="button" [disabled]="busy()" (click)="cancel()">{{ 'order.cancel' | t }}</button>
        }
      </div>
    }
  `,
})
export class OrderDetailComponent implements OnInit {
  private readonly i18n = inject(I18nService);
  private readonly orders = inject(OrdersService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);
  readonly busy = signal(false);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly order = signal<OrderDetailDto | null>(null);
  readonly labels = ORDER_STATUS_LABELS;
  readonly pending = OrderStatus.Pending;

  ngOnInit(): void {
    this.loading.set(true); this.failed.set(false);
    const id = this.route.snapshot.paramMap.get('orderId')!;
    this.orders.getClient(id).subscribe({ next: (o) => { this.order.set(o); this.loading.set(false); }, error: () => { this.failed.set(true); this.loading.set(false); } });
  }

  async cancel(): Promise<void> {
    const id = this.order()?.id;
    if (!id || this.busy()) return;
    if (!(await this.confirm.ask('order.confirmCancel'))) return;
    this.busy.set(true);
    this.orders.cancelClient(id).subscribe({
      next: () => { this.toast.success(this.i18n.t('order.cancelled')); this.busy.set(false); this.ngOnInit(); },
      error: () => this.busy.set(false),
    });
  }
}
