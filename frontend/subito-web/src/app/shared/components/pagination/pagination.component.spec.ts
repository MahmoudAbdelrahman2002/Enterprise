import { TestBed } from '@angular/core/testing';
import { I18nService } from '../../../core/services/i18n.service';
import { PaginationComponent } from './pagination.component';

describe('Shared page navigation', () => {
  beforeEach(() => TestBed.configureTestingModule({ imports: [PaginationComponent], providers: [
    { provide: I18nService, useValue: { t: (key: string) => key === 'pagination.goTo' ? 'Go to page {page}' : key } },
  ] }));
  it('labels the current page, bounds navigation, and supports direct page selection', () => {
    const fixture = TestBed.createComponent(PaginationComponent);
    fixture.componentRef.setInput('page', 4); fixture.componentRef.setInput('totalPages', 12); fixture.componentRef.setInput('totalCount', 240);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('nav').getAttribute('aria-label')).toBe('pagination.navigation');
    expect(fixture.nativeElement.querySelector('[aria-current="page"]').textContent.trim()).toBe('4');
    const changed = jasmine.createSpy(); fixture.componentInstance.change.subscribe(changed);
    fixture.componentInstance.select(0); fixture.componentInstance.select(13); fixture.componentInstance.select(4); fixture.componentInstance.select(NaN);
    expect(changed).not.toHaveBeenCalled();
    fixture.nativeElement.querySelector('[aria-label="Go to page 12"]').click(); expect(changed).toHaveBeenCalledOnceWith(12);
  });
  it('blocks repeat navigation while loading and hides unnecessary controls on a single page', () => {
    const fixture = TestBed.createComponent(PaginationComponent);
    fixture.componentRef.setInput('totalPages', 2); fixture.componentRef.setInput('disabled', true); fixture.detectChanges();
    expect([...fixture.nativeElement.querySelectorAll('button')].every((button: any) => button.disabled)).toBeTrue();
    const changed = jasmine.createSpy(); fixture.componentInstance.change.subscribe(changed); fixture.componentInstance.select(2);
    expect(changed).not.toHaveBeenCalled();
    fixture.componentRef.setInput('totalPages', 1); fixture.componentRef.setInput('disabled', false); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('nav')).toBeNull();
  });
  it('keeps focus on a control the user selected while the next page was loading', () => {
    const fixture = TestBed.createComponent(PaginationComponent);
    fixture.componentRef.setInput('totalPages', 2); fixture.detectChanges();
    const pageButton = fixture.nativeElement.querySelector('[aria-label="Go to page 2"]') as HTMLButtonElement;
    pageButton.focus(); pageButton.click(); fixture.componentRef.setInput('disabled', true); fixture.detectChanges();
    const other = document.createElement('button'); document.body.append(other);
    try {
      other.focus(); fixture.componentRef.setInput('page', 2); fixture.componentRef.setInput('disabled', false); fixture.detectChanges();
      expect(document.activeElement).toBe(other);
    } finally { other.remove(); }
  });
});
