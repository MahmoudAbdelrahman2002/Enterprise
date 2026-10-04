import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { fieldRules } from '../../../shared/forms/field-validators';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { Component, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { ClientMarketServiceDto } from '../../../core/models/domain.models';
import { CatalogService } from '../../../core/services/catalog.service';
import { I18nService } from '../../../core/services/i18n.service';
import { readPage, resolvePage } from '../../../core/utils/read-list';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [PaginationComponent, IconComponent, RouterLink, ReactiveFormsModule, TranslatePipe, EmptyStateComponent],
  template: `
    <section class="hero">
      <div class="hero-copy">
        <p class="eyebrow">{{ 'home.eyebrow' | t }}</p>
        <h1>{{ 'home.hero' | t }}</h1>
        <p class="lede">{{ 'home.lede' | t }}</p>
        <a class="btn btn-accent hero-cta" href="#services">{{ 'home.services' | t }}<app-icon class="directional" name="right" /></a>
        <div class="hero-steps"><span>01 · {{ 'home.services' | t }}</span><span>02 · {{ 'home.browseStores' | t }}</span><span>03 · {{ 'actions.checkout' | t }}</span></div>
      </div>
      <div class="hero-visual" aria-hidden="true">
        <img src="/assets/logo-mark-light.png" alt="" />
      </div>
    </section>

    <section class="section" id="services">
      <div class="section-head">
        <h2 class="section-title">{{ 'home.services' | t }}</h2>
        <p class="muted">{{ 'home.servicesHint' | t }}</p>
      </div>

      @if (loading()) {
        <div class="grid-cards">
          @for (_ of skeletons; track $index) {
            <div class="card service-card">
              <div class="thumb-lg skeleton"></div>
              <div class="skeleton line"></div>
              <div class="skeleton line short"></div>
            </div>
          }
        </div>
      } @else if (failed()) {
      <div class="card stack error-state" role="alert"><p>{{ 'errors.generic' | t }}</p><button class="btn btn-ghost" type="button" (click)="reload()">{{ 'actions.retry' | t }}</button></div>
    } @else if (!services().length) {
        <app-empty-state messageKey="home.noServices" />
      } @else {
        <div class="grid-cards">
          @for (s of services(); track s.id) {
            <a class="card service-card" [routerLink]="['/services', s.id, 'providers']">
              @if (s.imageUrl) {
                <img class="thumb-lg" [src]="s.imageUrl" [alt]="s.name || ''" loading="lazy" />
              } @else {
                <div class="thumb-lg media-fallback">{{ (s.name || '?').slice(0, 1) }}</div>
              }
              <h3>{{ s.name }}</h3>
              @if (s.description) {
                <p class="muted clamp">{{ s.description }}</p>
              }
              <span class="cta">{{ 'home.browseStores' | t }} <app-icon class="directional" name="right" /></span>
            </a>
          }
        </div>
      }
    </section>
    <app-pagination [page]="page()" [totalPages]="totalPages()" [totalCount]="totalCount()" [disabled]="loading()" labelKey="pagination.services" (change)="load($event)" />
  `,
  styles: [
    `
      .hero {
        display: grid;
        grid-template-columns: minmax(0, 1.3fr) minmax(0, 0.7fr);
        gap: 1.5rem;
        align-items: center;
        padding: clamp(1.25rem, 3vw, 2.25rem);
        margin-bottom: 1.75rem;
        border-radius: 24px;
        background:
          radial-gradient(circle at 85% 20%, rgba(0, 199, 177, 0.28), transparent 40%),
          linear-gradient(135deg, #10243a 0%, #1a3352 55%, #0d3d3a 100%);
        color: #fff;
        overflow: hidden;
        min-height: 310px;
      }
      .hero-cta { margin-top: 1.5rem; } .hero-steps { display: flex; flex-wrap: wrap; gap: .5rem 1.25rem; margin-top: 1.5rem; font-size: .75rem; color: #c8e4e6; }
      .eyebrow {
        margin: 0 0 0.5rem;
        font-size: 0.8rem;
        font-weight: 800;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        color: #9af0e4;
      }
      .hero h1 {
        margin: 0 0 0.65rem;
        font-family: var(--subito-display);
        font-size: clamp(1.7rem, 4vw, 2.6rem);
        letter-spacing: -0.04em;
        line-height: 1.15;
        max-width: 14ch;
      }
      .lede {
        margin: 0;
        color: rgba(255, 255, 255, 0.78);
        max-width: 36ch;
        line-height: 1.5;
      }
      .hero-visual {
        display: grid;
        place-items: center;
      }
      .hero-visual img {
        width: min(210px, 42vw);
        filter: drop-shadow(0 16px 30px rgba(0, 0, 0, 0.25));
      }
      .section-head {
        margin-bottom: 1rem;
      }
      .service-card {
        display: flex;
        flex-direction: column;
        gap: 0.55rem;
        transition: transform 0.15s ease, box-shadow 0.15s ease;
        padding: 0.85rem;
      }
      .service-card:hover {
        transform: translateY(-2px);
        box-shadow: var(--subito-shadow);
      }
      .service-card h3 {
        margin: 0.15rem 0 0;
        font-family: var(--subito-display);
        font-size: 1.05rem;
        letter-spacing: -0.02em;
      }
      .clamp {
        margin: 0;
        display: -webkit-box;
        -webkit-line-clamp: 2;
        -webkit-box-orient: vertical;
        overflow: hidden;
        min-height: 2.5em;
      }
      .cta {
        margin-top: auto;
        font-weight: 800;
        color: var(--subito-navy);
        font-size: 0.9rem;
      }
      .line {
        height: 14px;
        margin-top: 0.75rem;
      }
      .line.short {
        width: 55%;
      }
      @media (max-width: 720px) {
        .hero {
          grid-template-columns: 1fr;
          text-align: start;
        }
        .hero-visual {
          order: -1;
        }
        .hero-visual img {
          width: 96px;
        }
        .hero h1 {
          max-width: none;
        }
      }
    `,
  ],
})
export class HomeComponent {
  private readonly catalog = inject(CatalogService);
  private readonly i18n = inject(I18nService);
  private readonly route = inject(ActivatedRoute);
  readonly services = signal<ClientMarketServiceDto[]>([]);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly search = new FormControl('', { nonNullable: true, validators: fieldRules.search });
  readonly skeletons = [1, 2, 3, 4, 5, 6];
  readonly page = signal(1);
  readonly totalPages = signal(1);
  readonly totalCount = signal(0);
  private readonly query = signal('');
  private readonly retryCount = signal(0);

  constructor() {
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe((params) => {
      const q = params.get('q') || '';
      if (this.search.value !== q) {
        this.search.setValue(q, { emitEvent: false });
      }
      this.query.set(q); this.page.set(1);
    });
    this.search.valueChanges.pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed()).subscribe(() => { if (this.search.valid) { this.query.set(this.search.value); this.page.set(1); } });
    effect((onCleanup) => {
      this.i18n.lang();
      const query = this.query();
      const page = this.page();
      this.retryCount();
      this.loading.set(true); this.failed.set(false);
      const request = this.catalog.listClientServices(12, query || null, page).subscribe({
        next: (data) => {
          const result = readPage<ClientMarketServiceDto>(data);
          const targetPage = resolvePage(page, result);
          if (page !== targetPage) { this.page.set(targetPage); return; }
          this.services.set(result.items); this.totalPages.set(result.totalPages); this.totalCount.set(result.totalCount); this.loading.set(false);
        },
        error: () => { this.failed.set(true); this.loading.set(false); },
      });
      onCleanup(() => request.unsubscribe());
    });
  }

  load(page: number): void { this.page.set(page); }

  reload(): void {
    this.retryCount.update(value => value + 1);
  }
}
