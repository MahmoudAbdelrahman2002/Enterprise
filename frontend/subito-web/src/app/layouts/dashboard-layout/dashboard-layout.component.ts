import { TooltipDirective } from '../../shared/directives/tooltip.directive';
import { IconComponent } from '../../shared/components/icon/icon.component';
import { UpperCasePipe } from '@angular/common';
import { Component, Input, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { Portal } from '../../core/models/api.models';
import { AuthService } from '../../core/services/auth.service';
import { I18nService, Lang } from '../../core/services/i18n.service';
import { TokenStoreService } from '../../core/services/token-store.service';
import { LogoComponent } from '../../shared/components/logo/logo.component';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

export interface NavItem {
  labelKey: string;
  link: string;
  permissions?: string[];
}

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [TooltipDirective, IconComponent, RouterLink, RouterLinkActive, LogoComponent, TranslatePipe, UpperCasePipe],
  template: `
    <div class="dash" [class.collapsed]="collapsed()">
      <aside class="sidebar" [class.open]="drawerOpen()">
        <div class="side-head">
          <app-logo [link]="homeLink" [height]="34" [light]="true" [markOnly]="collapsed()" />
          <button class="icon-btn hide-desktop" type="button" (click)="drawerOpen.set(false)" appTooltip [attr.aria-label]="'ui.closeMenu' | t"><app-icon name="close" /></button>
        </div>
        <nav class="side-nav">
          @for (item of visibleItems(); track item.link) {
            <a
              [routerLink]="item.link"
              routerLinkActive="active"
              [routerLinkActiveOptions]="item.link === homeLink ? homeActiveOptions : defaultActiveOptions"
              (click)="drawerOpen.set(false)"
            >
              <app-icon [name]="navIcon(item.labelKey)" /><span>{{ item.labelKey | t }}</span>
            </a>
          }
        </nav>
        <div class="side-foot">
          <div class="lang">
            @for (l of langs; track l) {
              <button type="button" class="lang-btn" [class.active]="i18n.lang() === l" [attr.aria-pressed]="i18n.lang() === l" appTooltip [attr.aria-label]="('lang.' + l) | t" (click)="i18n.setLang(l)">
                {{ l | uppercase }}
              </button>
            }
          </div>
          <button class="logout-btn" type="button" (click)="auth.logout(portal)">{{ 'nav.logout' | t }}</button>
        </div>
      </aside>
      <div class="main">
        <header class="top">
          <button class="btn btn-ghost menu-toggle" type="button" (click)="toggleNav()" [attr.aria-expanded]="drawerOpen() || !collapsed()" appTooltip [attr.aria-label]="'ui.openMenu' | t"><app-icon name="menu" /></button>
          <div class="top-title">
            <strong>{{ title }}</strong>
            <span class="muted hide-sm">{{ userLabel }}</span>
          </div>
        </header>
        <div class="content">
          <ng-content></ng-content>
        </div>
      </div>
      @if (drawerOpen()) {
        <div class="backdrop" (click)="drawerOpen.set(false)"></div>
      }
    </div>
  `,
  styles: [
    `
      .dash {
        min-height: 100vh;
        display: grid;
        grid-template-columns: 268px 1fr;
        background: var(--subito-page);
      }
      .dash.collapsed {
        grid-template-columns: 88px 1fr;
      }
      .sidebar {
        background:
          radial-gradient(circle at top left, rgba(0, 199, 177, 0.18), transparent 42%),
          linear-gradient(180deg, #10243a 0%, #0c1b2c 100%);
        color: #fff;
        display: flex;
        flex-direction: column;
        padding: 1.1rem 0.9rem;
        position: sticky;
        top: 0;
        height: 100vh;
        border-inline-end: 1px solid rgba(0, 199, 177, 0.18);
      }
      .side-head {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 1.35rem;
        padding-inline: 0.35rem;
      }
      .icon-btn {
        border: 1px solid rgba(255, 255, 255, 0.2);
        background: rgba(255, 255, 255, 0.06);
        color: #fff;
        border-radius: 10px;
        min-width: 44px;
        min-height: 44px;
        cursor: pointer;
      }
      .side-nav {
        display: flex;
        flex-direction: column;
        gap: 0.3rem;
        flex: 1;
        overflow: auto;
      }
      .side-nav a {
        display: flex; align-items: center; gap: .75rem; min-height: 48px;
        padding: 0.72rem 0.9rem;
        border-radius: 12px;
        color: rgba(255, 255, 255, 0.78);
        font-weight: 600;
        border: 1px solid transparent;
        transition: background 0.15s ease, color 0.15s ease, border-color 0.15s ease;
      }
      .side-nav a:hover {
        background: rgba(0, 199, 177, 0.12);
        color: #fff;
        border-color: rgba(0, 199, 177, 0.22);
      }
      .side-nav a.active {
        background: var(--subito-teal);
        color: var(--subito-navy);
        border-color: transparent;
        font-weight: 800;
        box-shadow: 0 8px 18px rgba(0, 199, 177, 0.22);
      }
      .side-foot {
        display: grid;
        gap: 0.75rem;
        margin-top: 1rem;
        padding-top: 0.85rem;
        border-top: 1px solid rgba(255, 255, 255, 0.1);
      }
      .lang {
        display: inline-flex;
        gap: 0.25rem;
      }
      .lang-btn {
        border: 0;
        background: transparent;
        color: rgba(255, 255, 255, 0.7);
        cursor: pointer;
        padding: 0.35rem 0.5rem;
        border-radius: 8px;
        font-size: 0.72rem;
        font-weight: 800;
        min-height: 44px;
        min-width: 44px;
      }
      .lang-btn.active {
        background: var(--subito-teal);
        color: var(--subito-navy);
      }
      .logout-btn {
        border: 1px solid rgba(255, 255, 255, 0.18);
        background: rgba(255, 255, 255, 0.04);
        color: #fff;
        border-radius: 999px;
        min-height: 44px;
        font-weight: 700;
        cursor: pointer;
        transition: 0.15s ease;
      }
      .logout-btn:hover {
        background: rgba(0, 199, 177, 0.16);
        border-color: rgba(0, 199, 177, 0.4);
      }
      .main {
        min-width: 0;
        display: flex;
        flex-direction: column;
      }
      .top {
        display: flex;
        align-items: center;
        gap: 0.85rem;
        padding: 0.85rem 1.25rem;
        border-bottom: 1px solid var(--subito-border);
        background: rgba(255, 255, 255, 0.92);
        backdrop-filter: blur(10px);
        position: sticky;
        top: 0;
        z-index: 20;
        box-shadow: 0 1px 0 rgba(0, 199, 177, 0.12);
      }
      .top-title {
        display: flex;
        flex-wrap: wrap;
        align-items: baseline;
        gap: 0.55rem 1rem;
      }
      .top-title strong {
        font-family: var(--subito-display);
        letter-spacing: -0.02em;
        color: var(--subito-navy);
      }
      .menu-toggle {
        min-width: 44px;
      }
      .content {
        padding: 1.25rem;
      }
      .hide-desktop {
        display: none;
      }
      .backdrop {
        display: none;
      }
      .dash.collapsed .side-nav a {
        flex-direction: column; gap: .25rem;
        text-align: center;
        padding-inline: 0.4rem;
        font-size: 0.72rem;
      }
      @media (max-width: 960px) {
        .dash,
        .dash.collapsed {
          grid-template-columns: 1fr;
        }
        .sidebar {
          position: fixed;
          inset-block: 0;
          inset-inline-start: 0;
          width: min(290px, 88vw);
          transform: translateX(-105%);
          transition: transform 0.2s ease;
          z-index: 40;
        }
        :host-context([dir='rtl']) .sidebar {
          transform: translateX(105%);
        }
        .sidebar.open,
        :host-context([dir='rtl']) .sidebar.open {
          transform: translateX(0);
        }
        .backdrop {
          display: block;
          position: fixed;
          inset: 0;
          background: rgba(16, 36, 58, 0.45);
          z-index: 30;
        }
        .hide-desktop {
          display: inline-flex;
        }
      }
    `,
  ],
})
export class DashboardLayoutComponent {
  @Input({ required: true }) portal!: Portal;
  @Input({ required: true }) title!: string;
  @Input({ required: true }) homeLink!: string;
  @Input({ required: true }) items: NavItem[] = [];

  readonly tokens = inject(TokenStoreService);
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);
  readonly langs: Lang[] = ['en', 'ar', 'it'];
  readonly collapsed = signal(false);
  readonly drawerOpen = signal(false);
  /** Stable refs — new objects each CD cycle break routerLinkActive. */
  readonly homeActiveOptions = { exact: true } as const;
  readonly defaultActiveOptions = { exact: false } as const;

  navIcon(key: string): import('../../shared/components/icon/icon.component').IconName {
    const names: Record<string, import('../../shared/components/icon/icon.component').IconName> = {
      'nav.dashboard': 'grid', 'nav.store': 'store', 'nav.providers': 'store', 'nav.categories': 'grid',
      'nav.services': 'grid', 'nav.products': 'package', 'nav.orders': 'receipt', 'nav.notifications': 'bell',
      'nav.roles': 'user', 'nav.staff': 'user', 'nav.users': 'user', 'nav.clients': 'user', 'nav.profile': 'user',
    }; return names[key] ?? 'grid';
  }
  get userLabel(): string {
    const u = this.tokens.getUser(this.portal);
    return u ? `${u.firstName} ${u.lastName}` : '';
  }

  visibleItems(): NavItem[] {
    return this.items.filter(
      (i) => !i.permissions?.length || this.tokens.hasAnyPermission(this.portal, i.permissions)
    );
  }

  toggleNav(): void {
    if (window.innerWidth <= 960) {
      this.drawerOpen.update((v) => !v);
    } else {
      this.collapsed.update((v) => !v);
    }
  }
}
