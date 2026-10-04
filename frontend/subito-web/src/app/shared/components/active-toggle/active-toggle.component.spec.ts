import { TestBed } from '@angular/core/testing';
import { ConfirmService } from '../../../core/services/confirm.service';
import { I18nService } from '../../../core/services/i18n.service';
import { ActiveToggleComponent } from './active-toggle.component';

describe('Active toggle safeguards', () => {
  let ask: jasmine.Spy;
  beforeEach(() => {
    ask = jasmine.createSpy().and.resolveTo(false);
    TestBed.configureTestingModule({ imports: [ActiveToggleComponent], providers: [
      { provide: ConfirmService, useValue: { ask } }, { provide: I18nService, useValue: { t: (key: string) => key } },
    ] });
  });
  it('names the affected record and requires confirmation before deactivation', async () => {
    const fixture = TestBed.createComponent(ActiveToggleComponent);
    const component = fixture.componentInstance;
    component.active = true; component.targetName = 'Example Store'; component.confirmationKey = 'confirm.deactivateProvider';
    const changed = jasmine.createSpy(); component.changed.subscribe(changed);
    await component.toggle();
    expect(ask).toHaveBeenCalledWith('confirm.deactivateProvider', 'Example Store', 'actions.deactivate');
    expect(changed).not.toHaveBeenCalled();
    ask.and.resolveTo(true); await component.toggle();
    expect(changed).toHaveBeenCalledTimes(1);
  });
  it('activates immediately and blocks disabled controls', async () => {
    const component = TestBed.createComponent(ActiveToggleComponent).componentInstance;
    const changed = jasmine.createSpy(); component.changed.subscribe(changed);
    await component.toggle(); component.disabled = true; await component.toggle();
    expect(ask).not.toHaveBeenCalled(); expect(changed).toHaveBeenCalledTimes(1);
  });
  it('prevents duplicate prompts while a decision is pending', async () => {
    const component = TestBed.createComponent(ActiveToggleComponent).componentInstance;
    component.active = true; component.targetName = 'Staff';
    let resolve!: (value: boolean) => void;
    ask.and.returnValue(new Promise<boolean>(done => resolve = done));
    const first = component.toggle(); await component.toggle();
    expect(ask).toHaveBeenCalledTimes(1); resolve(false); await first;
    expect(component.confirming()).toBeFalse();
  });
});
