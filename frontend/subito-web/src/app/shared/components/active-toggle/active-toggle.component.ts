import { Component, EventEmitter, Input, Output, inject, signal } from '@angular/core';
import { ConfirmService } from '../../../core/services/confirm.service';
import { TranslatePipe } from '../../pipes/translate.pipe';

@Component({
  selector: 'app-active-toggle', standalone: true, imports: [TranslatePipe],
  template: `<button type="button" class="active-switch" role="switch" [class.on]="active" [attr.aria-checked]="active" [disabled]="disabled || confirming()" [attr.aria-label]="label || (('ui.active' | t) + (targetName ? ': ' + targetName : ''))" [title]="(active ? 'actions.deactivate' : 'actions.activate') | t" (click)="toggle()"><span></span></button>`,
  styles: [`:host{display:inline-flex}.active-switch{width:44px;height:26px;padding:3px;border:0;border-radius:20px;background:#7a8791;cursor:pointer;display:flex;align-items:center}.active-switch span{width:20px;height:20px;border-radius:50%;background:white;transition:transform .15s}.active-switch.on{background:var(--subito-teal-dark)}.active-switch.on span{transform:translateX(18px)}:host-context([dir=rtl]) .active-switch.on span{transform:translateX(-18px)}.active-switch:focus-visible{outline:2px solid var(--subito-navy);outline-offset:3px}.active-switch:disabled{opacity:.55;cursor:not-allowed}`],
})
export class ActiveToggleComponent {
  @Input() active = false;
  @Input() disabled = false;
  @Input() label = '';
  @Input() targetName = '';
  @Input() confirmationKey = 'confirm.deactivate';
  readonly confirming = signal(false);
  private readonly confirm = inject(ConfirmService);
  @Output() changed = new EventEmitter<void>();
  async toggle(): Promise<void> {
    if (this.disabled || this.confirming()) return;
    if (this.active && this.targetName) {
      this.confirming.set(true);
      try {
        if (!(await this.confirm.ask(this.confirmationKey, this.targetName, 'actions.deactivate'))) return;
        if (this.disabled || !this.active) return;
      } finally { this.confirming.set(false); }
    }
    this.changed.emit();
  }
}
