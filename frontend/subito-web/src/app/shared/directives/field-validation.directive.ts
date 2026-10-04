import { AfterViewInit, Directive, DoCheck, ElementRef, OnDestroy, Renderer2, inject } from '@angular/core';
import { NgControl, Validators } from '@angular/forms';
import { I18nService } from '../../core/services/i18n.service';
import { firstErrorKey } from '../forms/form-errors';
import { VALIDATION_POLICY as P } from '../forms/validation-policy';

let nextErrorId = 0;
@Directive({ selector: '[appFieldValidation]', standalone: true })
export class FieldValidationDirective implements AfterViewInit, DoCheck, OnDestroy {
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private readonly renderer = inject(Renderer2);
  private readonly field = inject(NgControl, { self: true });
  private readonly i18n = inject(I18nService);
  private messageElement?: HTMLElement;
  private readonly errorId = `field-validation-${++nextErrorId}`;
  private originalDescription = '';
  private requirementElement?: HTMLElement;

  ngAfterViewInit(): void {
    const message = this.renderer.createElement('small');
    this.messageElement = message;
    this.renderer.addClass(message, 'field-error');
    this.renderer.setAttribute(message, 'id', this.errorId);
    this.renderer.setAttribute(message, 'aria-live', 'polite');
    this.renderer.setProperty(message, 'hidden', true);
    const anchor = (this.element.parentElement?.classList.contains('password-field') || this.element.parentElement?.classList.contains('search-field')) ? this.element.parentElement : this.element;
    this.renderer.insertBefore(anchor!.parentNode, message, anchor!.nextSibling);
    this.originalDescription = this.element.getAttribute('aria-describedby') ?? '';
    const label = this.element.closest('.field')?.querySelector('label');
    if (label && this.element.hasAttribute('formControlName')) {
      this.requirementElement = this.renderer.createElement('span');
      this.renderer.addClass(this.requirementElement, 'field-requirement');
      this.renderer.appendChild(label, this.requirementElement);
    }
    this.ngDoCheck();
  }

  ngDoCheck(): void {
    if (!this.messageElement) return;
    const control = this.field.control;
    const required = control?.hasValidator(Validators.required) ?? false;
    this.renderer.setAttribute(this.element, 'aria-required', String(required));
    if (this.requirementElement) {
      this.renderer.setProperty(this.requirementElement, 'textContent', ` (${this.i18n.t(required ? 'validation.requiredLabel' : 'validation.optionalLabel')})`);
    }
    const name = String(this.field.name ?? this.element.getAttribute('formControlName') ?? '');
    let key = firstErrorKey(control);
    if (control?.touched && name === 'newPassword' && control.parent?.errors?.['passwordDifferent']) key = 'validation.passwordDifferent';
    if (key === 'validation.format' && name === 'otp') key = 'validation.otp';
    if (key === 'validation.format' && (name === 'code' || name === 'sku')) key = 'validation.code';
    const errors = control?.errors ?? {};
    let message = key ? errors['server'] || this.i18n.t(key) : '';
    const values = { max: errors['maxlength']?.requiredLength ?? errors['max']?.max ?? errors['integerRange']?.max ?? P.PriceMax, min: errors['minlength']?.requiredLength ?? errors['min']?.min ?? errors['integerRange']?.min ?? 0, digits: P.OtpLength };
    for (const [token, value] of Object.entries(values)) message = message.replaceAll(`{${token}}`, String(value));
    this.renderer.setProperty(this.messageElement, 'textContent', message);
    this.renderer.setProperty(this.messageElement, 'hidden', !message);
    this.renderer.setAttribute(this.element, 'aria-invalid', String(!!message));
    const description = [this.originalDescription, message ? this.errorId : ''].filter(Boolean).join(' ');
    if (description) this.renderer.setAttribute(this.element, 'aria-describedby', description);
    else this.renderer.removeAttribute(this.element, 'aria-describedby');
  }

  ngOnDestroy(): void {
    if (this.requirementElement?.parentNode) this.renderer.removeChild(this.requirementElement.parentNode, this.requirementElement);
    if (this.messageElement?.parentNode) this.renderer.removeChild(this.messageElement.parentNode, this.messageElement);
  }
}
