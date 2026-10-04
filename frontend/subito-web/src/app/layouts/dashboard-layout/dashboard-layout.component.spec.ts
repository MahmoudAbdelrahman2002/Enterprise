import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { I18nService } from '../../core/services/i18n.service';
import { TokenStoreService } from '../../core/services/token-store.service';
import { DashboardLayoutComponent } from './dashboard-layout.component';

describe('Dashboard navigation accessibility', () => {
  let fixture: ComponentFixture<DashboardLayoutComponent>;
  let mobile: boolean;
  let page: HTMLElement;
  let sidebar: HTMLElement;
  let trigger: HTMLButtonElement;
  let close: HTMLButtonElement;

  beforeEach(async () => {
    mobile = true;
    spyOn(window, 'matchMedia').and.callFake(() => ({ matches: mobile } as MediaQueryList));
    await TestBed.configureTestingModule({
      imports: [DashboardLayoutComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { logout: jasmine.createSpy('logout') } },
        { provide: TokenStoreService, useValue: { getUser: () => null, hasAnyPermission: () => true } },
        { provide: I18nService, useValue: { lang: signal('en'), t: (key: string) => key, setLang: () => {} } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(DashboardLayoutComponent);
    fixture.componentRef.setInput('portal', 'admin');
    fixture.componentRef.setInput('title', 'Admin');
    fixture.componentRef.setInput('homeLink', '/admin');
    fixture.componentRef.setInput('items', [{ labelKey: 'nav.providers', link: '/admin/providers' }]);
    fixture.detectChanges();
    page = fixture.nativeElement;
    sidebar = page.querySelector('.sidebar')!;
    trigger = page.querySelector('.menu-toggle')!;
    close = page.querySelector('.hide-desktop')!;
    // Exercise mobile controls even when Karma's viewport uses desktop CSS.
    close.style.display = 'inline-flex';
  });

  afterEach(() => fixture.destroy());

  function open(): void {
    trigger.focus();
    trigger.click();
    fixture.detectChanges();
  }

  it('enters the mobile drawer and prevents focus on covered controls', () => {
    expect(sidebar.inert).toBeTrue();
    expect(trigger.getAttribute('aria-expanded')).toBe('false');
    open();
    expect(document.activeElement).toBe(close);
    expect(sidebar.getAttribute('role')).toBe('dialog');
    expect(sidebar.getAttribute('aria-modal')).toBe('true');
    expect(sidebar.inert).toBeFalse();
    const main = page.querySelector<HTMLElement>('.main')!;
    const background = document.createElement('button');
    main.append(background);
    expect(main.inert).toBeTrue();
    background.focus();
    expect(document.activeElement).toBe(close);
  });

  it('wraps forward and reverse Tab around usable drawer controls', () => {
    open();
    const first = sidebar.querySelector<HTMLElement>('a')!;
    const last = sidebar.querySelector<HTMLElement>('.logout-btn')!;
    last.focus();
    const forward = new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true });
    last.dispatchEvent(forward);
    expect(forward.defaultPrevented).toBeTrue();
    expect(document.activeElement).toBe(first);
    const backward = new KeyboardEvent('keydown', { key: 'Tab', shiftKey: true, bubbles: true, cancelable: true });
    first.dispatchEvent(backward);
    expect(backward.defaultPrevented).toBeTrue();
    expect(document.activeElement).toBe(last);
  });

  for (const action of ['Escape', 'close button', 'backdrop', 'navigation link', 'logo']) {
    it(`restores trigger focus after closing via ${action}`, () => {
      open();
      if (action === 'Escape') close.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
      else if (action === 'close button') close.click();
      else if (action === 'backdrop') page.querySelector<HTMLElement>('.backdrop')!.click();
      else sidebar.querySelector<HTMLElement>(action === 'logo' ? 'app-logo a' : '.side-nav a')!.click();
      fixture.detectChanges();
      expect(fixture.componentInstance.drawerOpen()).toBeFalse();
      expect(document.activeElement).toBe(trigger);
      expect(page.querySelector<HTMLElement>('.main')!.inert).toBeFalse();
      expect(sidebar.inert).toBeTrue();
    });
  }

  it('clears modal state on desktop resize and preserves desktop collapse', () => {
    open();
    mobile = false;
    window.dispatchEvent(new Event('resize'));
    fixture.detectChanges();
    expect(sidebar.inert).toBeFalse();
    expect(sidebar.getAttribute('aria-modal')).toBeNull();
    expect(page.querySelector<HTMLElement>('.main')!.inert).toBeFalse();
    trigger.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.collapsed()).toBeTrue();
    expect(fixture.componentInstance.drawerOpen()).toBeFalse();
    expect(sidebar.inert).toBeFalse();
  });

  it('moves focus out of navigation when resize hides the closed mobile drawer', () => {
    mobile = false;
    window.dispatchEvent(new Event('resize'));
    fixture.detectChanges();
    sidebar.querySelector<HTMLElement>('.lang-btn')!.focus();
    mobile = true;
    window.dispatchEvent(new Event('resize'));
    fixture.detectChanges();
    expect(document.activeElement).toBe(trigger);
    expect(sidebar.inert).toBeTrue();
  });

  it('keeps all shared badge variants above 4.5:1 contrast', () => {
    const luminance = (color: string): number => {
      const channels = color.match(/\d+/g)!.slice(0, 3).map((value) => {
        const channel = Number(value) / 255;
        return channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4;
      });
      return channels[0] * 0.2126 + channels[1] * 0.7152 + channels[2] * 0.0722;
    };
    for (const variant of ['', 'badge-success', 'badge-danger', 'badge-count']) {
      const badge = document.createElement('span');
      badge.className = `badge ${variant}`;
      badge.textContent = 'Active';
      page.append(badge);
      const style = getComputedStyle(badge);
      const foreground = luminance(style.color);
      const background = luminance(style.backgroundColor);
      const contrast = (Math.max(foreground, background) + 0.05) / (Math.min(foreground, background) + 0.05);
      expect(contrast).withContext(variant || 'badge').toBeGreaterThanOrEqual(4.5);
    }
  });
});
