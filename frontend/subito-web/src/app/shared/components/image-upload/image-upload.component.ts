import { imageError } from '../../forms/image-validation';
import { I18nService } from '../../../core/services/i18n.service';
import { ToastService } from '../../../core/services/toast.service';
import { Component, EventEmitter, Input, Output, signal, inject } from '@angular/core';
import { TranslatePipe } from '../../pipes/translate.pipe';
import { IconComponent } from '../icon/icon.component';

@Component({
  selector: 'app-image-upload', standalone: true, imports: [TranslatePipe, IconComponent],
  template: `<label class="upload" [title]="filename() || ('ui.uploadImage' | t)" [class.disabled]="disabled"><app-icon name="upload" /><input class="sr-only" type="file" accept="image/jpeg,image/png,image/webp" [disabled]="disabled" [attr.aria-label]="'ui.uploadImage' | t" (change)="choose($event)" /></label>`,
  styles: [`:host{display:inline-flex;max-width:100%}.upload{position:relative;display:flex;align-items:center;gap:.6rem;min-height:44px;max-width:100%;padding:.65rem .9rem;border:1px dashed var(--subito-teal-dark);border-radius:12px;color:var(--subito-navy);background:var(--subito-teal-soft);cursor:pointer;font-weight:700;font-size:.85rem}.upload:focus-within{outline:2px solid var(--subito-navy);outline-offset:3px}.upload span{overflow:hidden;text-overflow:ellipsis;white-space:nowrap;max-width:180px}.disabled{opacity:.55;cursor:not-allowed}`],
})
export class ImageUploadComponent {
  @Input() disabled = false;
  @Output() selected = new EventEmitter<File>();
  readonly filename = signal('');
  private readonly i18n = inject(I18nService);
  private readonly toast = inject(ToastService);
  async choose(event: Event): Promise<void> {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file || this.disabled) return;
    const error = await imageError(file);
    if (error) {
      this.toast.error(this.i18n.t(error));
      (event.target as HTMLInputElement).value = '';
      return;
    }
    if (this.disabled) return;
    this.filename.set(file.name);
    this.selected.emit(file);
    (event.target as HTMLInputElement).value = '';
  }
}
