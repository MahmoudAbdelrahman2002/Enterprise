import { Directive, ElementRef, HostListener, inject } from '@angular/core';
/** Reuse the translated accessible name for a tooltip on hover and focus. */
@Directive({ selector: '[appTooltip]', standalone: true })
export class TooltipDirective {
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);
  @HostListener('mouseenter') @HostListener('focus')
  show(): void {
    this.element.nativeElement.setAttribute('data-tooltip', this.element.nativeElement.getAttribute('aria-label') ?? '');
  }
  @HostListener('mouseleave') @HostListener('blur') @HostListener('click')
  hide(): void { this.element.nativeElement.removeAttribute('data-tooltip'); }
}
