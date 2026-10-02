import { I18nService } from '../../../core/services/i18n.service';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ORDER_STATUS_LABELS, OrderStatus } from '../../../core/models/api.models';
import { OrderDetailDto, OrderListItemDto } from '../../../core/models/domain.models';
import { OrdersService } from '../../../core/services/orders.service';
import { readPage } from '../../../core/utils/read-list';
import { ToastService } from '../../../core/services/toast.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-provider-orders',
  standalone: true,
  imports: [MoneyPipe, FormsModule, RouterLink, DatePipe, EmptyStateComponent, PaginationComponent, TranslatePipe],
  template: `
    <h1 class="page-title">{{ 'nav.orders' | t }}</h1>
    @if (!selected()) {
      @if (loading()) { <p class="muted">{{ 'loading' | t }}</p> }
      @else if (failed()) { <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost" type="button" (click)="load(1)">{{ 'actions.retry' | t }}</button></div> } @else if (!items().length) { <app-empty-state /> } @else {
        <div class="table-wrap card"><table class="data">
          <thead><tr><th>{{ 'ui.date' | t }}</th><th>{{ 'ui.total' | t }}</th><th>{{ 'ui.status' | t }}</th><th></th></tr></thead>
          <tbody>
            @for (o of items(); track o.id) {
              <tr>
                <td>{{ o.orderDateUtc | date:'medium' }}</td>
                <td>{{ o.totalAmount | money }}</td>
                <td>{{ labels[o.status] | t }}</td>
                <td><a [routerLink]="['/provider/orders', o.id]">{{ 'actions.view' | t }}</a></td>
              </tr>
            }
          </tbody>
        </table></div>
        <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" (change)="load($event)" />
      }
    } @else {
      <div class="card stack">
        <a routerLink="/provider/orders">{{ 'actions.back' | t }}</a>
        <div class="row" style="justify-content:space-between">
          <span>{{ selected()!.orderDateUtc | date:'medium' }}</span>
          <span class="badge">{{ labels[selected()!.status] | t }}</span>
        </div>
        @for (i of selected()!.items; track i.id) {
          <div class="row" style="justify-content:space-between"><span>{{ i.productName }} × {{ i.quantity }}</span><span>{{ i.lineTotal | money }}</span></div>
        }
        <strong>{{ selected()!.totalAmount | money }}</strong>
        @if (canUpdate && nextStatuses.length) {
          <div class="row">
            <label for="order-next-status">{{ 'ui.status' | t }}</label><select id="order-next-status" [(ngModel)]="nextStatus" name="nextStatus" [disabled]="busy()">
              @for (s of nextStatuses; track s) { <option [ngValue]="s">{{ labels[s] | t }}</option> }
            </select>
            <button class="btn btn-primary" type="button" [disabled]="busy()" (click)="updateStatus()">{{ 'order.updateStatus' | t }}</button>
          </div>
        }
      </div>
    }
  `,
})
export class ProviderOrdersComponent implements OnInit {
  private readonly i18n = inject(I18nService);
  private readonly orders = inject(OrdersService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly tokens = inject(TokenStoreService);
  readonly items = signal<OrderListItemDto[]>([]);
  readonly selected = signal<OrderDetailDto | null>(null);
  readonly labels = ORDER_STATUS_LABELS;
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly busy = signal(false);
  page = 1; totalPages = 1; totalCount = 0; nextStatus = OrderStatus.Accepted;
  canUpdate = this.tokens.hasPermission('provider', 'ProviderOrder.Update');
  // Mirror Enterprise.Domain OrderStatusTransitions; the API remains authoritative.
  get nextStatuses(): OrderStatus[] {
    const allowed: Record<number, OrderStatus[]> = {
      [OrderStatus.Pending]: [OrderStatus.Accepted, OrderStatus.Cancelled],
      [OrderStatus.Accepted]: [OrderStatus.Preparing, OrderStatus.Cancelled],
      [OrderStatus.Preparing]: [OrderStatus.Ready, OrderStatus.Cancelled],
      [OrderStatus.Ready]: [OrderStatus.Completed, OrderStatus.Cancelled],
    };
    return allowed[this.selected()?.status ?? -1] ?? [];
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('orderId');
    if (id) {
      this.orders.getProvider(id).subscribe({
        next: (o) => { this.selected.set(o); this.nextStatus = this.nextStatuses[0] ?? o.status as OrderStatus; this.loading.set(false); },
        error: () => { this.failed.set(true); this.loading.set(false); },
      });
    } else {
      this.load(1);
    }
  }
  load(page: number): void {
    this.page = page;
    this.loading.set(true); this.failed.set(false);
    this.orders.listProvider({ pageNumber: page, pageSize: 20 }).subscribe({
      next: (r) => {
        const pageData = readPage<OrderListItemDto>(r);
        this.items.set(pageData.items);
        this.totalPages = pageData.totalPages;
        this.totalCount = pageData.totalCount;
        this.loading.set(false);
      },
      error: () => { this.failed.set(true); this.loading.set(false); },
    });
  }
  updateStatus(): void {
    const id = this.selected()?.id;
    if (!id || !this.canUpdate || this.busy() || !this.nextStatuses.includes(this.nextStatus)) return;
    this.busy.set(true);
    this.orders.updateProviderStatus(id, this.nextStatus).subscribe({
      next: () => { this.toast.success(this.i18n.t('ui.updated')); this.busy.set(false); this.ngOnInit(); },
      error: () => this.busy.set(false),
    });
  }
}
