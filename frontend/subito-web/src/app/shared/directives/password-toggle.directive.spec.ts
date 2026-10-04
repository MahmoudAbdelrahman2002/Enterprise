import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { I18nService } from '../../core/services/i18n.service';
import { PasswordToggleDirective } from './password-toggle.directive';

@Component({ standalone: true, imports: [PasswordToggleDirective], template: '<form><input id="password" type="password" appPasswordToggle value="secret" /></form>' })
class HostComponent {}

describe('PasswordToggleDirective', () => {
  it('reveals and hides a password without changing its value or submitting the form', () => {
    TestBed.configureTestingModule({ imports: [HostComponent], providers: [{ provide: I18nService, useValue: { t: (key: string) => key } }] });
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const input: HTMLInputElement = fixture.nativeElement.querySelector('input');
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button.type).toBe('button');
    expect(button.getAttribute('aria-controls')).toBe('password');
    button.click();
    fixture.detectChanges();
    expect(input.type).toBe('text');
    expect(input.value).toBe('secret');
    expect(button.getAttribute('aria-label')).toBe('auth.hidePassword');
    button.click();
    fixture.detectChanges();
    expect(input.type).toBe('password');
    expect(button.getAttribute('aria-pressed')).toBe('false');
  });
});
