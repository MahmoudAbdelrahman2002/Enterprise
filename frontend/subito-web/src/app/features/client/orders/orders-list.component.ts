import { PageRequest } from '../../../core/utils/page-request';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ORDER_STATUS_LABELS } from '../../../core/models/api.models';
import { OrderListItemDto } from '../../../core/models/domain.models';
import { OrdersService } from '../../../core/services/orders.service';
import { readPage, resolvePage } from '../../../core/utils/read-list';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-orders-list',
  standalone: true,
  imports: [MoneyPipe, RouterLink, DatePipe, EmptyStateComponent, PaginationComponent, TranslatePipe],
  template: `
    <h1 class="page-title">{{ 'nav.orders' | t }}</h1>
    @if (loading()) {
      <div class="stack">
        @for (_ of [1, 2, 3]; track $index) {
          <div class="card skeleton" style="height:88px"></div>
        }
      </div>
    } @else if (failed()) {
      <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost" type="button" (click)="load(1)">{{ 'actions.retry' | t }}</button></div>
    } @else if (!items().length) {
      <app-empty-state messageKey="empty.clientOrders" icon="receipt">
        <a class="btn btn-primary" routerLink="/">{{ 'home.browseStores' | t }}</a>
      </app-empty-state>
    } @else {
      <div class="orders stack">
        @for (o of items(); track o.id) {
          <a class="card order-card" [routerLink]="['/orders', o.id]">
            <div>
              <strong>{{ o.orderDateUtc | date: 'medium' }}</strong>
              <p class="muted">#{{ o.id.slice(0, 8) }}</p>
            </div>
            <div class="order-meta">
              <span class="badge">{{ (o.isHistorical ? 'order.historical' : labels[o.status]) | t }}</span>
              <span class="price">{{ o.totalAmount | money }}</span>
            </div>
          </a>
        }
      </div>
    }
    <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" [disabled]="loading()" (change)="load($event)" />
  `,
  styles: [
    `
      .order-card {
        display: flex;
        flex-wrap: wrap;
        justify-content: space-between;
        gap: 0.85rem;
        align-items: center;
        transition: box-shadow 0.15s ease, transform 0.15s ease;
      }
      .order-card:hover {
        box-shadow: var(--subito-shadow);
        transform: translateY(-1px);
      }
      .order-card .muted {
        margin: 0.2rem 0 0;
        font-size: 0.85rem;
      }
      .order-meta {
        display: flex;
        align-items: center;
        gap: 0.85rem;
      }
    `,
  ],
})
export class OrdersListComponent implements OnInit {
  private readonly pageRequest = new PageRequest(inject(DestroyRef));
  private readonly orders = inject(OrdersService);
  readonly items = signal<OrderListItemDto[]>([]);
  readonly labels = ORDER_STATUS_LABELS;
  readonly loading = signal(true);
  readonly failed = signal(false);
  page = 1;
  totalPages = 1;
  totalCount = 0;
  ngOnInit(): void {
    this.load(1);
  }
  load(page: number): void {
    this.page = page;
    this.loading.set(true); this.failed.set(false);
    this.pageRequest.run(this.orders.listClient({ pageNumber: page, pageSize: 10 }), {
      next: (res) => {
        const pageData = readPage<OrderListItemDto>(res);
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
