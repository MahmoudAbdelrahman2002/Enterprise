import { Component, computed, input, signal } from '@angular/core';
import { OrderItemDto } from '../../../core/models/domain.models';
import { MoneyPipe } from '../../pipes/money.pipe';
import { PaginationComponent } from '../pagination/pagination.component';

@Component({
  selector: 'app-order-items', standalone: true, imports: [MoneyPipe, PaginationComponent],
  template: `
    <div class="stack">
      @for (item of visibleItems(); track item.id) {
        <div class="row" style="justify-content:space-between"><span>{{ item.productName }} × {{ item.quantity }}</span><span>{{ item.lineTotal | money }}</span></div>
      }
    </div>
    <app-pagination [page]="currentPage()" [totalPages]="totalPages()" [totalCount]="items().length" labelKey="pagination.orderItems" (change)="page.set($event)" />
  `,
})
export class OrderItemsComponent {
  readonly items = input.required<OrderItemDto[]>();
  readonly page = signal(1);
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.items().length / 10)));
  readonly currentPage = computed(() => Math.min(this.page(), this.totalPages()));
  readonly visibleItems = computed(() => this.items().slice((this.currentPage() - 1) * 10, this.currentPage() * 10));
}
