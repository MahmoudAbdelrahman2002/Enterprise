import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, Subject } from 'rxjs';
import { CatalogService } from '../../../core/services/catalog.service';
import { I18nService } from '../../../core/services/i18n.service';
import { HomeComponent } from './home.component';

describe('Marketplace request lifecycle', () => {
  it('loads once initially, cancels stale searches and reloads once per language change', () => {
    const params = new BehaviorSubject(convertToParamMap({ q: 'food' }));
    const first = new Subject<unknown>(); const second = new Subject<unknown>(); const third = new Subject<unknown>();
    const list = jasmine.createSpy().and.returnValues(first, second, third);
    const lang = signal('en');
    TestBed.configureTestingModule({ imports: [HomeComponent], providers: [provideRouter([]),
      { provide: ActivatedRoute, useValue: { queryParamMap: params } },
      { provide: CatalogService, useValue: { listClientServices: list } },
      { provide: I18nService, useValue: { lang, t: (key: string) => key } },
    ] });
    const fixture = TestBed.createComponent(HomeComponent); fixture.detectChanges();
    expect(list).toHaveBeenCalledTimes(1); expect(list).toHaveBeenCalledWith(12, 'food', 1);
    params.next(convertToParamMap({ q: 'books' })); fixture.detectChanges();
    expect(list).toHaveBeenCalledTimes(2); expect(first.observed).toBeFalse();
    second.next([]); fixture.detectChanges(); expect(fixture.componentInstance.loading()).toBeFalse();
    lang.set('it'); fixture.detectChanges(); expect(list).toHaveBeenCalledTimes(3);
    expect(second.observed).toBeFalse(); fixture.destroy(); expect(third.observed).toBeFalse();
  });
});
