import { TokenStoreService } from '../../../core/services/token-store.service';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { CatalogService } from '../../../core/services/catalog.service';
import { ProvidersService } from '../../../core/services/providers.service';
import { readPage } from '../../../core/utils/read-list';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-home',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  template: `
    <section class="dash-hero card">
      <div>
        <p class="eyebrow">{{ 'nav.admin' | t }}</p>
        <h1 class="page-title">{{ 'dashboard.adminTitle' | t }}</h1>
        <p class="muted">{{ 'dashboard.adminHint' | t }}</p>
      </div>
    </section>

    @if (loading()) {
      <div class="stat-grid">
        @for (_ of [1, 2, 3]; track $index) {
          <div class="stat-card skeleton" style="height:110px"></div>
        }
      </div>
    } @else {
      <div class="stat-grid">
        @if (tokens.hasPermission('admin', 'Providers.Read')) { <a class="stat-card" routerLink="/admin/providers">
          <span class="stat-label">{{ 'nav.providers' | t }}</span>
          <strong class="stat-value">{{ counts().providers }}</strong>
        </a> }
        @if (tokens.hasPermission('admin', 'Services.Read')) { <a class="stat-card" routerLink="/admin/services">
          <span class="stat-label">{{ 'nav.services' | t }}</span>
          <strong class="stat-value">{{ counts().services }}</strong>
        </a> }
        @if (tokens.hasPermission('admin', 'Clients.Read')) { <a class="stat-card" routerLink="/admin/clients">
          <span class="stat-label">{{ 'nav.clients' | t }}</span>
          <strong class="stat-value">{{ counts().clients }}</strong>
        </a> }
      </div>
    }
  `,
  styles: [
    `
      .dash-hero {
        margin-bottom: 1rem;
        background:
          radial-gradient(circle at top right, rgba(0, 199, 177, 0.16), transparent 45%),
          #fff;
        border-color: #d7efe9;
      }
      .eyebrow {
        margin: 0 0 0.35rem;
        font-size: 0.78rem;
        font-weight: 800;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        color: var(--subito-teal-dark);
      }
      .page-title {
        margin-bottom: 0.35rem;
      }
      .stat-card {
        display: flex;
        flex-direction: column;
        gap: 0.55rem;
        padding: 1.15rem 1.2rem;
        border-radius: var(--subito-radius);
        background: #fff;
        border: 1px solid var(--subito-border);
        box-shadow: var(--subito-shadow-sm);
        transition: transform 0.15s ease, box-shadow 0.15s ease, border-color 0.15s ease;
        position: relative;
        overflow: hidden;
      }
      .stat-card::before {
        content: '';
        position: absolute;
        inset-block: 0;
        inset-inline-start: 0;
        width: 4px;
        background: var(--subito-teal);
      }
      .stat-card:hover {
        transform: translateY(-2px);
        box-shadow: var(--subito-shadow);
        border-color: #b7ebe4;
      }
      .stat-label {
        color: var(--subito-muted);
        font-weight: 700;
        font-size: 0.9rem;
      }
      .stat-value {
        font-family: var(--subito-display);
        font-size: 1.85rem;
        letter-spacing: -0.03em;
        color: var(--subito-navy);
      }
    `,
  ],
})
export class AdminHomeComponent implements OnInit {
  readonly tokens = inject(TokenStoreService);
  private readonly providers = inject(ProvidersService);
  private readonly catalog = inject(CatalogService);
  readonly loading = signal(true);
  readonly counts = signal({ providers: 0, services: 0, clients: 0 });

  ngOnInit(): void {
    const query = { pageNumber: 1, pageSize: 1 };
    forkJoin({
      providers: this.tokens.hasPermission('admin', 'Providers.Read') ? this.providers.list(query) : of(null),
      services: this.tokens.hasPermission('admin', 'Services.Read') ? this.catalog.listAdminServices(query) : of(null),
      clients: this.tokens.hasPermission('admin', 'Clients.Read') ? this.providers.listClients(query) : of(null),
    }).subscribe({
      next: (res) => {
        this.counts.set({
          providers: readPage(res.providers).totalCount,
          services: readPage(res.services).totalCount,
          clients: readPage(res.clients).totalCount,
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
