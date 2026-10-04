import { OrderItemsComponent } from '../../../shared/components/order-items/order-items.component';
import { PageRequest } from '../../../core/utils/page-request';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ORDER_STATUS_LABELS } from '../../../core/models/api.models';
import { OrderDetailDto, OrderListItemDto } from '../../../core/models/domain.models';
import { OrdersService } from '../../../core/services/orders.service';
import { readPage, resolvePage } from '../../../core/utils/read-list';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [OrderItemsComponent, IconComponent, MoneyPipe, RouterLink, DatePipe, EmptyStateComponent, PaginationComponent, TranslatePipe],
  template: `
    <h1 class="page-title">{{ 'nav.orders' | t }}</h1>
    @if (!selected()) {
      @if (loading()) { <p class="muted">{{ 'loading' | t }}</p> }
      @else if (failed()) { <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost icon-action" type="button" (click)="load(1)" [attr.aria-label]="'actions.retry' | t" [title]="'actions.retry' | t"><app-icon name="retry" /></button></div> } @else if (!items().length) { <app-empty-state messageKey="empty.adminOrders" icon="receipt" /> } @else {
        <div class="table-wrap card"><table class="data">
          <thead><tr><th>{{ 'ui.date' | t }}</th><th>{{ 'nav.provider' | t }}</th><th>{{ 'ui.total' | t }}</th><th>{{ 'ui.status' | t }}</th><th></th></tr></thead>
          <tbody>
            @for (o of items(); track o.id) {
              <tr>
                <td>{{ o.orderDateUtc | date:'medium' }}</td>
                <td>{{ o.providerId }}</td>
                <td>{{ o.totalAmount | money }}</td>
                <td>{{ labels[o.status] | t }}</td>
                <td><a [routerLink]="['/admin/orders', o.id]" [attr.aria-label]="'actions.view' | t" [title]="'actions.view' | t" class="btn btn-ghost icon-action"><app-icon name="eye" /></a></td>
              </tr>
            }
          </tbody>
        </table></div>
      }
    } @else {
      <div class="card stack">
        <a routerLink="/admin/orders" [attr.aria-label]="'actions.back' | t" [title]="'actions.back' | t" class="btn btn-ghost icon-action"><app-icon name="left" /></a>
        <span class="badge">{{ labels[selected()!.status] | t }}</span>
        <app-order-items [items]="selected()!.items" />
        <strong>{{ selected()!.totalAmount | money }}</strong>
      </div>
    }
    @if (!selected()) { <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" [disabled]="loading()" (change)="load($event)" /> }
  `,
})
export class AdminOrdersComponent implements OnInit {
  private readonly pageRequest = new PageRequest(inject(DestroyRef));
  private readonly orders = inject(OrdersService);
  private readonly route = inject(ActivatedRoute);
  readonly items = signal<OrderListItemDto[]>([]);
  readonly selected = signal<OrderDetailDto | null>(null);
  readonly labels = ORDER_STATUS_LABELS;
  readonly loading = signal(true);
  readonly failed = signal(false);
  page = 1; totalPages = 1; totalCount = 0;
  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('orderId');
    if (id) {
      this.orders.getAdmin(id).subscribe({
        next: (o) => { this.selected.set(o); this.loading.set(false); },
        error: () => { this.failed.set(true); this.loading.set(false); },
      });
    } else this.load(1);
  }
  load(page: number): void {
    this.page = page;
    this.loading.set(true); this.failed.set(false);
    this.pageRequest.run(this.orders.listAdmin({ pageNumber: page, pageSize: 20 }), {
      next: (r) => {
        const pageData = readPage<OrderListItemDto>(r);
        const targetPage = resolvePage(page, pageData);
        if (page !== targetPage) { this.load(targetPage); return; }
        this.items.set(pageData.items);
        this.totalPages = pageData.totalPages;
        this.totalCount = pageData.totalCount;
        this.loading.set(false);
      },
      error: () => { this.failed.set(true); this.loading.set(false); },
    });
  }
}
