import { Pipe, PipeTransform, inject } from '@angular/core';
import { I18nService } from '../../core/services/i18n.service';
// Keep aligned with StripeSettings.Currency. DTOs currently have no currency field.
export const CHECKOUT_CURRENCY = 'USD';
@Pipe({ name: 'money', standalone: true, pure: false })
export class MoneyPipe implements PipeTransform {
  private readonly i18n = inject(I18nService);
  transform(value: number | null | undefined): string {
    return new Intl.NumberFormat(this.i18n.lang(), {
      style: 'currency', currency: CHECKOUT_CURRENCY, currencyDisplay: 'code',
      minimumFractionDigits: 2, maximumFractionDigits: 2,
    }).format(value ?? 0);
  }
}
