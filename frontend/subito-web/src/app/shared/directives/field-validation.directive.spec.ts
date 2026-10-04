import { Component } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TestBed } from '@angular/core/testing';
import { I18nService } from '../../core/services/i18n.service';
import { fieldRules } from '../forms/field-validators';
import { FieldValidationDirective } from './field-validation.directive';

@Component({standalone:true, imports:[ReactiveFormsModule,FieldValidationDirective], template:'<form [formGroup]="form"><div class="field"><label for="name">Name</label><input id="name" formControlName="firstName" appFieldValidation aria-describedby="hint" /></div><div class="field"><label for="phone">Phone</label><input id="phone" formControlName="phone" appFieldValidation /></div></form>'})
class ValidationHost {
  form = new FormGroup({firstName:new FormControl('',fieldRules.name),phone:new FormControl('',fieldRules.phone)});
}

describe('FieldValidationDirective', () => {
  it('identifies required and optional fields before submission', () => {
    TestBed.configureTestingModule({ imports: [ValidationHost], providers: [{ provide: I18nService, useValue: { t: (key: string) => key } }] });
    const fixture = TestBed.createComponent(ValidationHost);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('label[for="name"]').textContent).toContain('validation.requiredLabel');
    expect(fixture.nativeElement.querySelector('#name').getAttribute('aria-required')).toBe('true');
    expect(fixture.nativeElement.querySelector('label[for="phone"]').textContent).toContain('validation.optionalLabel');
    expect(fixture.nativeElement.querySelector('#phone').getAttribute('aria-required')).toBe('false');
    expect(fixture.componentInstance.form.untouched).toBeTrue();
  });
  it('shows errors after touch, links them accessibly, and clears them when corrected', () => {
    TestBed.configureTestingModule({imports:[ValidationHost],providers:[{provide:I18nService,useValue:{t:(key:string)=>key}}]});
    const fixture=TestBed.createComponent(ValidationHost);
    fixture.detectChanges();
    const input:HTMLInputElement=fixture.nativeElement.querySelector('input');
    const message:HTMLElement=fixture.nativeElement.querySelector('small');
    expect(message.hidden).toBeTrue();
    fixture.componentInstance.form.controls.firstName.markAsTouched();
    fixture.detectChanges();
    expect(message.textContent).toBe('validation.required');
    expect(input.getAttribute('aria-invalid')).toBe('true');
    expect(input.getAttribute('aria-describedby')).toContain('hint');
    expect(input.getAttribute('aria-describedby')).toContain(message.id);
    fixture.componentInstance.form.controls.firstName.setValue('Anne');
    fixture.detectChanges();
    expect(message.hidden).toBeTrue();
    expect(input.getAttribute('aria-describedby')).toBe('hint');
  });
});
