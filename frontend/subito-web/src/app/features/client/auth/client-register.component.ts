import { FieldValidationDirective } from '../../../shared/directives/field-validation.directive';
import { fieldRules } from '../../../shared/forms/field-validators';
import { I18nService } from '../../../core/services/i18n.service';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { applyServerError, firstErrorKey } from '../../../shared/forms/form-errors';
import { LogoComponent } from '../../../shared/components/logo/logo.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { OtpRecoveryComponent } from '../../../shared/components/otp-recovery/otp-recovery.component';
import { OtpRecoveryState } from '../../../shared/forms/otp-recovery';

@Component({
  selector: 'app-client-register',
  standalone: true,
  imports: [OtpRecoveryComponent, FieldValidationDirective, ReactiveFormsModule, RouterLink, LogoComponent, TranslatePipe],
  template: `
    <div class="auth-shell">
      <div class="card auth-card stack">
        <app-logo />
        <h1 class="page-title">{{ 'nav.register' | t }}</h1>
        @if (!otpSent()) {
          <form [formGroup]="form" (ngSubmit)="send()">
        @if (form.errors?.['server']) { <p class="field-error" role="alert">{{ form.errors?.['server'] }}</p> }
            <div class="field"><label for="client-register-firstName">{{ 'auth.firstName' | t }}</label><input id="client-register-firstName" [readOnly]="busy()" appFieldValidation formControlName="firstName" /></div>
            <div class="field"><label for="client-register-lastName">{{ 'auth.lastName' | t }}</label><input id="client-register-lastName" [readOnly]="busy()" appFieldValidation formControlName="lastName" /></div>
            <div class="field"><label for="client-register-email">{{ 'auth.email' | t }}</label><input id="client-register-email" type="email" [readOnly]="busy()" appFieldValidation formControlName="email" autocomplete="email" /></div>
            <button class="btn btn-primary" type="submit" [disabled]="busy() || recovery.seconds() > 0">{{ 'auth.sendOtp' | t }} @if (recovery.seconds() > 0) { ({{ recovery.seconds() }} {{ 'auth.secondsShort' | t }}) }</button>
          </form>
        } @else {
          <form [formGroup]="otpForm" (ngSubmit)="verify()">
        @if (otpForm.errors?.['server']) { <p class="field-error" role="alert">{{ otpForm.errors?.['server'] }}</p> }
            @if (devOtp()) { <p class="badge">{{ 'auth.devOtp' | t }}: {{ devOtp() }}</p> }
            <div class="field"><label for="client-register-otp">{{ 'auth.otp' | t }}</label><input id="client-register-otp" appFieldValidation formControlName="otp" inputmode="numeric" autocomplete="one-time-code" /></div>
            <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'auth.verify' | t }}</button>
          </form>
          <app-otp-recovery [email]="form.controls.email.value" [busy]="busy()" [seconds]="recovery.seconds()" (resend)="send()" (changeEmail)="changeEmail()" />
        }
        <p class="muted"><a routerLink="/auth/login">{{ 'nav.login' | t }}</a></p>
      </div>
    </div>
  `,
})
export class ClientRegisterComponent {
  private readonly i18n = inject(I18nService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  readonly form = this.fb.nonNullable.group({
    firstName: ['', fieldRules.name],
    lastName: ['', fieldRules.name],
    email: ['', fieldRules.email],
  });
  readonly otpForm = this.fb.nonNullable.group({ otp: ['', fieldRules.otp] });
  readonly otpSent = signal(false);
  readonly devOtp = signal<string | null>(null);
  readonly busy = signal(false);
  readonly recovery = new OtpRecoveryState();
  private readonly pendingEmails = new Set<string>();

  changeEmail(): void {
    if (this.busy()) return;
    this.otpSent.set(false);
    this.devOtp.set(null);
    this.otpForm.reset();
    this.form.setErrors(null);
    this.recovery.focus('#client-register-email');
  }

  error(name: 'firstName' | 'lastName' | 'email'): string | null {
    return firstErrorKey(this.form.controls[name]);
  }
  otpError(): string | null { return firstErrorKey(this.otpForm.controls.otp); }

  send(): void {
    if (this.busy() || this.recovery.seconds() > 0) return;
    this.form.setErrors(null);
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.busy.set(true);
    const email = this.form.controls.email.value;
    const normalizedEmail = email.trim().toLowerCase();
    // Registration creates the pending account once; login reissues its registration code.
    const request = this.pendingEmails.has(normalizedEmail)
      ? this.auth.clientLogin({ email })
      : this.auth.clientRegister(this.form.getRawValue());
    request.subscribe({
      next: (res) => {
        this.pendingEmails.add(normalizedEmail);
        this.recovery.start(); this.otpForm.reset(); this.otpSent.set(true);
        this.devOtp.set(res.developmentOtp ?? null); this.toast.success(res.message || this.i18n.t('auth.otpSent'));
        this.busy.set(false); this.recovery.focus('#client-register-otp');
      },
      error: (err) => { this.recovery.onError(err); applyServerError(this.otpSent() ? this.otpForm : this.form, err); this.busy.set(false); },
    });
  }

  verify(): void {
    if (this.busy()) return;
    this.otpForm.markAllAsTouched();
    if (this.otpForm.invalid) return;
    this.busy.set(true);
    this.auth.clientVerifyRegistration({ email: this.form.controls.email.value, otp: this.otpForm.controls.otp.value }).subscribe({
      next: () => { this.toast.success(this.i18n.t('auth.created')); void this.router.navigateByUrl('/'); },
      error: (err) => { applyServerError(this.otpForm, err); this.busy.set(false); },
    });
  }
}
