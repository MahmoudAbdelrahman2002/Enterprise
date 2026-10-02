import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { TranslatePipe } from '../../pipes/translate.pipe';
import { IconComponent } from '../icon/icon.component';

@Component({
  selector: 'app-image-upload', standalone: true, imports: [TranslatePipe, IconComponent],
  template: `<label class="upload" [class.disabled]="disabled"><app-icon name="upload" /><span>{{ filename() || ('actions.upload' | t) }}</span><input class="sr-only" type="file" accept="image/*" [disabled]="disabled" [attr.aria-label]="'ui.uploadImage' | t" (change)="choose($event)" /></label>`,
  styles: [`:host{display:inline-flex;max-width:100%}.upload{position:relative;display:flex;align-items:center;gap:.6rem;min-height:44px;max-width:100%;padding:.65rem .9rem;border:1px dashed var(--subito-teal-dark);border-radius:12px;color:var(--subito-navy);background:var(--subito-teal-soft);cursor:pointer;font-weight:700;font-size:.85rem}.upload:focus-within{outline:2px solid var(--subito-navy);outline-offset:3px}.upload span{overflow:hidden;text-overflow:ellipsis;white-space:nowrap;max-width:180px}.disabled{opacity:.55;cursor:not-allowed}`],
})
export class ImageUploadComponent {
  @Input() disabled = false;
  @Output() selected = new EventEmitter<Event>();
  readonly filename = signal('');
  choose(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file || this.disabled) return;
    this.filename.set(file.name);
    this.selected.emit(event);
  }
}
