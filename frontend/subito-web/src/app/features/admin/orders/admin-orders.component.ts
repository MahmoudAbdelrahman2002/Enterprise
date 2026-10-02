import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ORDER_STATUS_LABELS } from '../../../core/models/api.models';
import { OrderDetailDto, OrderListItemDto } from '../../../core/models/domain.models';
import { OrdersService } from '../../../core/services/orders.service';
import { readPage } from '../../../core/utils/read-list';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [MoneyPipe, RouterLink, DatePipe, EmptyStateComponent, PaginationComponent, TranslatePipe],
  template: `
    <h1 class="page-title">{{ 'nav.orders' | t }}</h1>
    @if (!selected()) {
      @if (loading()) { <p class="muted">{{ 'loading' | t }}</p> }
      @else if (failed()) { <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost" type="button" (click)="load(1)">{{ 'actions.retry' | t }}</button></div> } @else if (!items().length) { <app-empty-state /> } @else {
        <div class="table-wrap card"><table class="data">
          <thead><tr><th>{{ 'ui.date' | t }}</th><th>{{ 'nav.provider' | t }}</th><th>{{ 'ui.total' | t }}</th><th>{{ 'ui.status' | t }}</th><th></th></tr></thead>
          <tbody>
            @for (o of items(); track o.id) {
              <tr>
                <td>{{ o.orderDateUtc | date:'medium' }}</td>
                <td>{{ o.providerId }}</td>
                <td>{{ o.totalAmount | money }}</td>
                <td>{{ labels[o.status] | t }}</td>
                <td><a [routerLink]="['/admin/orders', o.id]">{{ 'actions.view' | t }}</a></td>
              </tr>
            }
          </tbody>
        </table></div>
        <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" (change)="load($event)" />
      }
    } @else {
      <div class="card stack">
        <a routerLink="/admin/orders">{{ 'actions.back' | t }}</a>
        <span class="badge">{{ labels[selected()!.status] | t }}</span>
        @for (i of selected()!.items; track i.id) {
          <div class="row" style="justify-content:space-between"><span>{{ i.productName }} × {{ i.quantity }}</span><span>{{ i.lineTotal | money }}</span></div>
        }
        <strong>{{ selected()!.totalAmount | money }}</strong>
      </div>
    }
  `,
})
export class AdminOrdersComponent implements OnInit {
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
    this.orders.listAdmin({ pageNumber: page, pageSize: 20 }).subscribe({
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
}
