import { AfterViewInit, Directive, ElementRef, OnDestroy, Renderer2, effect, inject, signal } from '@angular/core';
import { I18nService } from '../../core/services/i18n.service';
import { ICON_PATHS } from '../components/icon/icon.component';

@Directive({ selector: 'input[appPasswordToggle]', standalone: true })
export class PasswordToggleDirective implements AfterViewInit, OnDestroy {
  private readonly input = inject<ElementRef<HTMLInputElement>>(ElementRef).nativeElement;
  private readonly renderer = inject(Renderer2);
  private readonly i18n = inject(I18nService);
  private readonly visible = signal(false);
  private button?: HTMLButtonElement;
  private path?: SVGPathElement;
  private unlisten?: () => void;

  constructor() {
    effect(() => {
      const visible = this.visible();
      const label = this.i18n.t(visible ? 'auth.hidePassword' : 'auth.showPassword');
      if (this.button) {
        this.renderer.setAttribute(this.button, 'aria-label', label);
        this.renderer.setAttribute(this.button, 'title', label);
        this.renderer.setAttribute(this.button, 'aria-pressed', String(visible));
      }
      if (this.path) this.renderer.setAttribute(this.path, 'd', visible ? ICON_PATHS.eyeOff : ICON_PATHS.eye);
    });
  }

  ngAfterViewInit(): void {
    const wrapper = this.renderer.createElement('div');
    this.renderer.addClass(wrapper, 'password-field');
    this.renderer.insertBefore(this.input.parentNode, wrapper, this.input);
    this.renderer.appendChild(wrapper, this.input);
    const button = this.renderer.createElement('button') as HTMLButtonElement;
    this.button = button;
    this.renderer.setAttribute(button, 'type', 'button');
    this.renderer.addClass(button, 'password-toggle');
    this.renderer.setAttribute(button, 'aria-label', this.i18n.t('auth.showPassword'));
    this.renderer.setAttribute(button, 'title', this.i18n.t('auth.showPassword'));
    this.renderer.setAttribute(button, 'aria-pressed', 'false');
    if (this.input.id) this.renderer.setAttribute(button, 'aria-controls', this.input.id);
    const svg = this.renderer.createElement('svg', 'svg');
    for (const [key, value] of Object.entries({viewBox:'0 0 24 24', fill:'none', stroke:'currentColor', 'stroke-width':'1.8', 'stroke-linecap':'round', 'stroke-linejoin':'round', 'aria-hidden':'true'})) this.renderer.setAttribute(svg, key, value);
    this.path = this.renderer.createElement('path', 'svg');
    this.renderer.setAttribute(this.path, 'd', ICON_PATHS.eye);
    this.renderer.appendChild(svg, this.path);
    this.renderer.appendChild(button, svg);
    this.renderer.appendChild(wrapper, button);
    this.unlisten = this.renderer.listen(button, 'click', () => {
      this.visible.update(value => !value);
      this.renderer.setAttribute(this.input, 'type', this.visible() ? 'text' : 'password');
      this.input.focus();
    });
  }

  ngOnDestroy(): void { this.unlisten?.(); }
}
