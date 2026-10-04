import { Component, ElementRef, HostListener, afterRenderEffect, inject, OnDestroy } from '@angular/core';
import { ConfirmService } from '../../../core/services/confirm.service';
import { TranslatePipe } from '../../pipes/translate.pipe';
import { IconComponent } from '../icon/icon.component';
@Component({
  selector: 'app-confirm-host', standalone: true, imports: [TranslatePipe, IconComponent],
  template: `
    @if (confirm.request(); as req) {
      <div class="confirm-backdrop" (click)="confirm.answer(false)">
        <div class="card confirm-card stack" role="dialog" aria-modal="true" aria-labelledby="confirm-title" aria-describedby="confirm-message" (click)="$event.stopPropagation()">
          <h2 id="confirm-title" class="section-title">{{ 'confirm.title' | t }}</h2>
          <p id="confirm-message">@if (req.target) { <strong>{{ req.target }}</strong><br /> }{{ req.messageKey | t }}</p>
          <div class="row">
            <button class="btn btn-ghost" data-cancel type="button" (click)="confirm.answer(false)">{{ 'actions.cancel' | t }}</button>
            <button class="btn btn-danger" type="button" (click)="confirm.answer(true)"><app-icon [name]="req.actionKey === 'actions.delete' ? 'trash' : 'check'" />{{ req.actionKey | t }}</button>
          </div>
        </div>
      </div>
    }
  `,
})
export class ConfirmHostComponent implements OnDestroy {
  readonly confirm = inject(ConfirmService);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private previous: HTMLElement | null = null;
  constructor() {
    afterRenderEffect(() => {
      if (this.confirm.request()) {
        if (!this.previous) this.previous = document.activeElement as HTMLElement;
        this.host.nativeElement.querySelector<HTMLElement>('[data-cancel]')?.focus();
      } else if (this.previous) { this.previous.focus(); this.previous = null; }
    });
  }
  @HostListener('document:keydown', ['$event'])
  onKey(event: KeyboardEvent): void {
    if (!this.confirm.request()) return;
    if (event.key === 'Escape') { event.preventDefault(); this.confirm.answer(false); }
    if (event.key === 'Tab') {
      const buttons = this.host.nativeElement.querySelectorAll<HTMLButtonElement>('button');
      const first = buttons[0], last = buttons[buttons.length - 1];
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    }
  }
  ngOnDestroy(): void { this.previous?.focus(); }
}
