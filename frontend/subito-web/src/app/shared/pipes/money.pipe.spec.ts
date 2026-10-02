import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { I18nService, Lang } from '../../core/services/i18n.service';
import { MoneyPipe } from './money.pipe';

describe('MoneyPipe checkout currency', () => {
  it('keeps USD when the language changes, including Arabic', () => {
    const lang = signal<Lang>('en');
    TestBed.configureTestingModule({ providers: [{ provide: I18nService, useValue: { lang } }] });
    const pipe = TestBed.runInInjectionContext(() => new MoneyPipe());
    for (const locale of ['en', 'ar', 'it'] as Lang[]) {
      lang.set(locale);
      expect(pipe.transform(12.5)).toEqual(new Intl.NumberFormat(locale, {
        style: 'currency', currency: 'USD', currencyDisplay: 'code', minimumFractionDigits: 2, maximumFractionDigits: 2,
      }).format(12.5));
      expect(pipe.transform(12.5)).toContain('USD');
    }
  });
});
