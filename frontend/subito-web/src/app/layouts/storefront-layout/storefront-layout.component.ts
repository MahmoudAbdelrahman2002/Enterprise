import { TooltipDirective } from '../../shared/directives/tooltip.directive';
import { IconComponent } from '../../shared/components/icon/icon.component';
import { UpperCasePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { I18nService, Lang } from '../../core/services/i18n.service';
import { NotificationsService } from '../../core/services/notifications.service';
import { TokenStoreService } from '../../core/services/token-store.service';
import { LogoComponent } from '../../shared/components/logo/logo.component';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-storefront-layout',
  standalone: true,
  imports: [TooltipDirective, IconComponent, 
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    ReactiveFormsModule,
    LogoComponent,
    TranslatePipe,
    UpperCasePipe,
  ],
  template: `
    <header class="sf-header">
      <div class="container sf-bar">
        <div class="sf-brand">
          <app-logo [height]="42" class="logo-full hide-sm" />
          <app-logo [height]="32" [markOnly]="true" class="logo-mark show-sm" />
        </div>

        <form class="sf-search" (submit)="onSearch($event)" role="search">
          <label class="sr-only" for="global-search">{{ 'search.services' | t }}</label>
          <app-icon class="search-icon" name="search" />
          <input
            id="global-search"
            type="search"
            [formControl]="search"
            [placeholder]="'home.searchPlaceholder' | t"
            autocomplete="off"
          />
        </form>

        <div class="sf-actions">
          @if (tokens.isAuthenticated('client')) {
            <a class="icon-link hide-sm" routerLink="/orders" routerLinkActive="active" appTooltip [attr.aria-label]="'nav.orders' | t"><app-icon name="receipt" />
              <span>{{ 'nav.orders' | t }}</span>
            </a>
            <a class="icon-link cart-link" routerLink="/cart" routerLinkActive="active" appTooltip [attr.aria-label]="'nav.cart' | t"><app-icon name="basket" />
              <span class="hide-sm">{{ 'nav.cart' | t }}</span>
              @if (cart.itemCount() > 0) {
                <span class="badge badge-count">{{ cart.itemCount() }}</span>
              }
            </a>
            <a class="icon-link hide-sm" routerLink="/notifications" routerLinkActive="active" appTooltip [attr.aria-label]="'nav.notifications' | t"><app-icon name="bell" />
              <span class="sr-only">{{ 'nav.notifications' | t }}</span>
              @if (unread() > 0) {
                <span class="badge badge-danger">{{ unread() }}</span>
              }
            </a>
            <a class="icon-link hide-sm" routerLink="/profile" routerLinkActive="active" appTooltip [attr.aria-label]="'nav.profile' | t"><app-icon name="user" /><span class="sr-only">{{ 'nav.profile' | t }}</span></a>
            <button class="btn btn-ghost hide-sm" type="button" (click)="auth.logout('client', '/')">
              {{ 'nav.logout' | t }}
            </button>
          } @else {
            <a class="btn btn-ghost hide-sm" routerLink="/auth/login">{{ 'nav.login' | t }}</a>
            <a class="btn btn-accent hide-sm" routerLink="/auth/register">{{ 'nav.register' | t }}</a>
          }

          <div class="lang hide-sm">
            @for (l of langs; track l) {
              <button type="button" class="lang-btn" [class.active]="i18n.lang() === l" [attr.aria-pressed]="i18n.lang() === l" appTooltip [attr.aria-label]="('lang.' + l) | t" (click)="i18n.setLang(l)">
                {{ l | uppercase }}
              </button>
            }
          </div>

          <button
            class="menu-btn btn btn-ghost"
            type="button"
            (click)="menuOpen.set(!menuOpen())"
            [attr.aria-expanded]="menuOpen()"
            aria-controls="sf-mobile-nav" appTooltip [attr.aria-label]="(menuOpen() ? 'ui.closeMenu' : 'ui.openMenu') | t"
          >
            <app-icon [name]="menuOpen() ? 'close' : 'menu'" />
          </button>
        </div>
      </div>

      <nav id="sf-mobile-nav" class="sf-mobile" [class.open]="menuOpen()" [attr.aria-hidden]="!menuOpen()">
        <div class="container stack">
          <a routerLink="/" (click)="closeMenu()">{{ 'nav.home' | t }}</a>
          @if (tokens.isAuthenticated('client')) {
            <a routerLink="/orders" (click)="closeMenu()">{{ 'nav.orders' | t }}</a>
            <a routerLink="/cart" (click)="closeMenu()">{{ 'nav.cart' | t }}</a>
            <a routerLink="/notifications" (click)="closeMenu()">{{ 'nav.notifications' | t }}</a>
            <a routerLink="/profile" (click)="closeMenu()">{{ 'nav.profile' | t }}</a>
            <button class="btn btn-ghost" type="button" (click)="auth.logout('client', '/'); closeMenu()">
              {{ 'nav.logout' | t }}
            </button>
          } @else {
            <a routerLink="/auth/login" (click)="closeMenu()">{{ 'nav.login' | t }}</a>
            <a class="btn btn-accent" routerLink="/auth/register" (click)="closeMenu()">{{ 'nav.register' | t }}</a>
          }
          <div class="lang">
            @for (l of langs; track l) {
              <button type="button" class="lang-btn" [class.active]="i18n.lang() === l" [attr.aria-pressed]="i18n.lang() === l" appTooltip [attr.aria-label]="('lang.' + l) | t" (click)="i18n.setLang(l)">
                {{ l | uppercase }}
              </button>
            }
          </div>
        </div>
      </nav>
    </header>

    <a class="skip-link" href="#marketplace-main">{{ 'ui.skipContent' | t }}</a><main class="sf-main" id="marketplace-main" tabindex="-1">
      <div class="container">
        <router-outlet />
      </div>
    </main>

    <footer class="sf-footer">
      <div class="container footer-grid">
        <div>
          <app-logo [height]="28" />
          <p class="muted footer-copy">{{ 'home.tagline' | t }}</p>
        </div>
        <div class="muted">
          <span>© Subito</span>
        </div>
      </div>
    </footer>
  `,
  styles: [
    `
      .sr-only {
        position: absolute;
        width: 1px;
        height: 1px;
        padding: 0;
        margin: -1px;
        overflow: hidden;
        clip: rect(0, 0, 0, 0);
        border: 0;
      }
      .sf-header {
        position: sticky;
        top: 0;
        z-index: 50;
        background: rgba(255, 255, 255, 0.96);
        backdrop-filter: blur(12px);
        border-bottom: 1px solid var(--subito-border);
      }
      .sf-bar {
        display: grid;
        grid-template-columns: auto minmax(0, 1fr) auto;
        align-items: center;
        gap: 0.85rem;
        min-height: 88px;
        padding-block: 0.65rem;
      }
      .sf-brand {
        display: flex;
        align-items: center;
      }
      .show-sm {
        display: none;
      }
      .sf-search {
        position: relative;
        width: min(100%, 520px);
        justify-self: center;
      }
      .sf-search input {
        width: 100%;
        min-height: 44px;
        border: 1px solid var(--subito-border);
        border-radius: 999px;
        padding-block: .65rem; padding-inline: 2.6rem 1rem;
        background: #fff;
        color: var(--subito-text);
      }
      .sf-search input:focus {
        outline: none;
        border-color: var(--subito-teal);
        box-shadow: var(--subito-focus);
      }
      .search-icon {
        position: absolute;
        inset-inline-start: 0.9rem;
        top: 50%;
        width: 18px;
        height: 18px;
        transform: translateY(-50%);
        color: var(--subito-muted);
        pointer-events: none;
      }
      .sf-actions {
        justify-content: end;
        display: flex;
        align-items: center;
        gap: 0.45rem;
      }
      .icon-link {
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
        min-height: 44px;
        min-width: 44px;
        padding: 0.35rem 0.65rem;
        border-radius: 999px;
        font-weight: 700;
        color: var(--subito-navy);
      }
      .icon-link svg {
        width: 20px;
        height: 20px;
      }
      .icon-link.active,
      .icon-link:hover {
        background: var(--subito-teal-soft);
      }
      .cart-link {
        position: relative;
      }
      .lang {
        display: inline-flex;
        gap: 0.2rem;
        margin-inline-start: 0.25rem;
      }
      .lang-btn {
        border: 0;
        background: transparent;
        cursor: pointer;
        padding: 0.35rem 0.45rem;
        border-radius: 8px;
        font-size: 0.72rem;
        font-weight: 800;
        color: var(--subito-muted);
        min-height: 44px;
        min-width: 44px;
      }
      .lang-btn.active {
        background: var(--subito-navy);
        color: #fff;
      }
      .menu-btn {
        display: none;
        min-width: 44px;
      }
      .sf-mobile {
        display: none;
        border-top: 1px solid var(--subito-border);
        padding: 0.85rem 0 1rem;
        background: #fff;
      }
      .sf-mobile.open {
        display: block;
      }
      .sf-mobile a {
        display: flex;
        align-items: center;
        min-height: 44px;
        font-weight: 700;
      }
      .sf-main {
        padding: 1.25rem 0 3rem;
        min-height: calc(100vh - 160px);
      }
      .sf-footer {
        border-top: 1px solid var(--subito-border);
        padding: 1.5rem 0 2rem;
        background: #fff;
      }
      .footer-grid {
        display: flex;
        flex-wrap: wrap;
        justify-content: space-between;
        gap: 1rem;
        align-items: end;
      }
      .footer-copy {
        margin: 0.5rem 0 0;
        max-width: 320px;
      }
      @media (max-width: 860px) {
        .sf-bar {
          grid-template-columns: auto 1fr auto;
        }
        .logo-full {
          display: none;
        }
        .show-sm {
          display: inline-flex;
        }
        .hide-sm {
          display: none !important;
        }
        .menu-btn {
          display: inline-flex;
        }
        .sf-search {
          grid-column: 1 / -1;
          width: 100%;
          order: 3;
          justify-self: stretch;
        }
      }
    `,
  ],
})
export class StorefrontLayoutComponent implements OnInit {
  readonly tokens = inject(TokenStoreService);
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);
  readonly notifications = inject(NotificationsService);
  readonly cart = inject(CartService);
  private readonly router = inject(Router);

  readonly menuOpen = signal(false);
  readonly unread = signal(0);
  readonly langs: Lang[] = ['en', 'ar', 'it'];
  readonly search = new FormControl('', { nonNullable: true });

  ngOnInit(): void {
    if (this.tokens.isAuthenticated('client')) {
      this.cart.refreshCount();
      this.notifications.unreadCount().subscribe({
        next: (d) => this.unread.set(d.unreadCount),
        error: () => undefined,
      });
    }
    this.search.valueChanges.pipe(debounceTime(400), distinctUntilChanged()).subscribe((q) => {
      const term = q.trim();
      if (!term && !this.search.dirty) return;
      // Global search lands on home with query param; home already searches services.
      void this.router.navigate(['/'], { queryParams: term ? { q: term } : {} });
    });
  }

  onSearch(event: Event): void {
    event.preventDefault();
    const term = this.search.value.trim();
    void this.router.navigate(['/'], { queryParams: term ? { q: term } : {} });
    this.closeMenu();
  }

  closeMenu(): void {
    this.menuOpen.set(false);
  }
}
