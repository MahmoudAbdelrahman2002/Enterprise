import { Pipe, PipeTransform, inject } from '@angular/core';
import { I18nService } from '../../core/services/i18n.service';

@Pipe({ name: 'cartCount', standalone: true, pure: false })
export class CartCountPipe implements PipeTransform {
  private readonly i18n = inject(I18nService);
  transform(count: number, kind: 'product' | 'unit' = 'unit'): string {
    const prefix = `cart.${kind}.`;
    const plural = new Intl.PluralRules(this.i18n.lang()).select(count);
    return this.i18n.t(prefix + plural, this.i18n.t(prefix + 'other')).replaceAll('{count}', String(count));
  }
}
