const fs = require('fs');
const path = require('path');
const root = path.join('frontend', 'subito-web', 'src', 'app', 'features');

function write(rel, content) {
  const p = path.join(root, rel);
  fs.mkdirSync(path.dirname(p), { recursive: true });
  fs.writeFileSync(p, content);
  console.log('wrote', rel);
}

write(
  'client/home/home.component.ts',
  `import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ClientMarketServiceDto } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, TranslatePipe, EmptyStateComponent],
  template: \`
    <section class="hero card">
      <h1 class="page-title">{{ 'home.hero' | t }}</h1>
      <p class="muted">{{ 'home.services' | t }}</p>
    </section>
    @if (loading()) {
      <p class="muted">{{ 'loading' | t }}</p>
    } @else if (!services().length) {
      <app-empty-state />
    } @else {
      <div class="grid-cards" style="margin-top:1rem">
        @for (s of services(); track s.id) {
          <a class="card service" [routerLink]="['/services', s.id, 'providers']">
            @if (s.imageUrl) {
              <img class="thumb-lg" [src]="s.imageUrl" [alt]="s.name || 'service'" />
            } @else {
              <div class="thumb-lg"></div>
            }
            <h3>{{ s.name }}</h3>
            <p class="muted">{{ s.description }}</p>
          </a>
        }
      </div>
    }
  \`,
  styles: [\`.hero{margin-bottom:1rem;background:linear-gradient(135deg,#fff,#ffe8e0)}.service h3{margin:.75rem 0 .25rem;font-family:var(--subito-display);font-style:italic}\`],
})
export class HomeComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly services = signal<ClientMarketServiceDto[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.api.get<ClientMarketServiceDto[]>('/client/services').subscribe({
      next: (data) => { this.services.set(data ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }
}
`
);

write(
  'client/providers/providers-list.component.ts',
  `import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ClientProviderListItemDto, PagedResult } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-providers-list',
  standalone: true,
  imports: [RouterLink, FormsModule, EmptyStateComponent, PaginationComponent, TranslatePipe],
  template: \`
    <div class="toolbar">
      <h1 class="page-title">Stores</h1>
      <input [(ngModel)]="search" (keyup.enter)="load(1)" [placeholder]="'actions.search' | t" />
    </div>
    @if (!items().length) { <app-empty-state /> } @else {
      <div class="grid-cards">
        @for (p of items(); track p.id) {
          <a class="card" [routerLink]="['/stores', p.id]">
            @if (p.imageUrl) { <img class="thumb-lg" [src]="p.imageUrl" [alt]="p.companyName" /> } @else { <div class="thumb-lg"></div> }
            <h3>{{ p.companyName }}</h3>
            <p class="muted">{{ p.serviceName }}</p>
          </a>
        }
      </div>
      <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" (change)="load($event)" />
    }
  \`,
})
export class ProvidersListComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  readonly items = signal<ClientProviderListItemDto[]>([]);
  search = '';
  page = 1;
  totalPages = 1;
  totalCount = 0;
  serviceId = '';

  ngOnInit(): void {
    this.serviceId = this.route.snapshot.paramMap.get('serviceId') || '';
    this.load(1);
  }

  load(page: number): void {
    this.page = page;
    this.api.get<PagedResult<ClientProviderListItemDto>>(\`/client/services/\${this.serviceId}/providers\`, {
      pageNumber: page, pageSize: 12, searchTerm: this.search || null,
    }).subscribe({
      next: (res) => {
        this.items.set(res.items ?? []);
        this.totalPages = res.totalPages;
        this.totalCount = res.totalCount;
      },
    });
  }
}
`
);

write(
  'client/store/store.component.ts',
  `import { DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  ClientCategoryDto, ClientProductDto, ClientProviderListItemDto,
} from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { ToastService } from '../../../core/services/toast.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-store',
  standalone: true,
  imports: [RouterLink, TranslatePipe, EmptyStateComponent, DecimalPipe],
  template: \`
    @if (store()) {
      <section class="card store-head">
        <div class="row" style="justify-content:space-between">
          <div>
            <h1 class="page-title">{{ store()!.companyName }}</h1>
            <p class="muted">{{ store()!.serviceName }} · {{ store()!.phoneNumber }}</p>
          </div>
          <a class="btn btn-primary" [routerLink]="['/cart', providerId]">{{ 'nav.cart' | t }}</a>
        </div>
      </section>
    }
    <div class="row" style="margin:1rem 0; overflow:auto">
      <button class="btn" [class.btn-primary]="!categoryId" [class.btn-ghost]="!!categoryId" type="button" (click)="selectCategory(null)">All</button>
      @for (c of categories(); track c.id) {
        <button class="btn" [class.btn-primary]="categoryId===c.id" [class.btn-ghost]="categoryId!==c.id" type="button" (click)="selectCategory(c.id)">{{ c.name }}</button>
      }
    </div>
    @if (!products().length) { <app-empty-state /> } @else {
      <div class="grid-cards">
        @for (p of products(); track p.id) {
          <article class="card">
            <a [routerLink]="['/products', p.id]">
              @if (p.imageUrl) { <img class="thumb-lg" [src]="p.imageUrl" [alt]="p.name" /> } @else { <div class="thumb-lg"></div> }
              <h3>{{ p.name }}</h3>
            </a>
            <p class="muted">{{ p.description }}</p>
            <div class="row" style="justify-content:space-between">
              <strong>{{ p.price | number:'1.2-2' }}</strong>
              <button class="btn btn-primary" type="button" (click)="add(p)">{{ 'actions.addToCart' | t }}</button>
            </div>
          </article>
        }
      </div>
    }
  \`,
})
export class StoreComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly tokens = inject(TokenStoreService);
  private readonly toast = inject(ToastService);
  providerId = '';
  categoryId: string | null = null;
  readonly store = signal<ClientProviderListItemDto | null>(null);
  readonly categories = signal<ClientCategoryDto[]>([]);
  readonly products = signal<ClientProductDto[]>([]);

  ngOnInit(): void {
    this.providerId = this.route.snapshot.paramMap.get('providerId') || '';
    this.api.get<ClientProviderListItemDto>(\`/client/providers/\${this.providerId}\`).subscribe({
      next: (s) => this.store.set(s),
    });
    this.api.get<ClientCategoryDto[]>(\`/client/\${this.providerId}/categories\`).subscribe({
      next: (c) => this.categories.set(c ?? []),
    });
    this.loadProducts();
  }

  selectCategory(id: string | null): void {
    this.categoryId = id;
    this.loadProducts();
  }

  loadProducts(): void {
    this.api.get<ClientProductDto[]>(\`/client/\${this.providerId}/products\`, {
      categoryId: this.categoryId,
    }).subscribe({ next: (p) => this.products.set(p ?? []) });
  }

  add(p: ClientProductDto): void {
    if (!this.tokens.isAuthenticated('client')) {
      this.toast.info('Please sign in to add items');
      return;
    }
    this.api.post(\`/client/\${this.providerId}/cart/items\`, { productId: p.id, quantity: 1 }).subscribe({
      next: () => this.toast.success('Added to cart'),
    });
  }
}
`
);

console.log('ok');
