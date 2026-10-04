import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { I18nService } from '../../../core/services/i18n.service';
import { TeamFiltersComponent } from './team-filters.component';

describe('Team discovery controls', () => {
  beforeEach(() => TestBed.configureTestingModule({ imports: [TeamFiltersComponent], providers: [
    { provide: I18nService, useValue: { t: (key: string) => key } },
  ] }));
  it('debounces name/email search and preserves selected filters', fakeAsync(() => {
    const component = TestBed.createComponent(TeamFiltersComponent).componentInstance;
    const changed = jasmine.createSpy(); component.changed.subscribe(changed);
    component.status = 'inactive'; component.descending = true;
    component.search.setValue('A'); tick(100); component.search.setValue('Alice'); tick(299);
    expect(changed).not.toHaveBeenCalled(); tick(1);
    expect(changed).toHaveBeenCalledWith(jasmine.objectContaining({ searchTerm: 'Alice', isActive: false, descending: true }));
  }));
  it('clears all filters in one request and distinguishes custom roles', () => {
    const component = TestBed.createComponent(TeamFiltersComponent).componentInstance;
    const changed = jasmine.createSpy(); component.changed.subscribe(changed);
    component.kind = 'roles'; component.system = 'custom'; component.emit();
    expect(changed).toHaveBeenCalledWith(jasmine.objectContaining({ isSystem: false }));
    changed.calls.reset(); component.clear();
    expect(changed).toHaveBeenCalledTimes(1);
    expect(changed).toHaveBeenCalledWith({ searchTerm: null, roleId: null, isActive: null, isSystem: null, descending: false });
  });
});
