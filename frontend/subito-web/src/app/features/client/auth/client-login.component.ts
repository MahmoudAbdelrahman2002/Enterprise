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
  selector: 'app-client-login',
  standalone: true,
  imports: [OtpRecoveryComponent, FieldValidationDirective, ReactiveFormsModule, RouterLink, LogoComponent, TranslatePipe],
  template: `
    <div class="auth-shell">
      <div class="card auth-card stack">
        <app-logo />
        <h1 class="page-title">{{ 'nav.login' | t }}</h1>
        @if (!otpSent()) {
          <form [formGroup]="emailForm" (ngSubmit)="send()">
        @if (emailForm.errors?.['server']) { <p class="field-error" role="alert">{{ emailForm.errors?.['server'] }}</p> }
            <div class="field">
              <label for="email">{{ 'auth.email' | t }}</label>
              <input id="email" type="email" [readOnly]="busy()" appFieldValidation formControlName="email" autocomplete="email" />
            </div>
            <button class="btn btn-primary" type="submit" [disabled]="busy() || recovery.seconds() > 0">{{ 'auth.sendOtp' | t }} @if (recovery.seconds() > 0) { ({{ recovery.seconds() }} {{ 'auth.secondsShort' | t }}) }</button>
          </form>
        } @else {
          <form [formGroup]="otpForm" (ngSubmit)="verify()">
        @if (otpForm.errors?.['server']) { <p class="field-error" role="alert">{{ otpForm.errors?.['server'] }}</p> }
            @if (devOtp()) { <p class="badge">{{ 'auth.devOtp' | t }}: {{ devOtp() }}</p> }
            <div class="field">
              <label for="otp">{{ 'auth.otp' | t }}</label>
              <input id="otp" appFieldValidation formControlName="otp" inputmode="numeric" autocomplete="one-time-code" />
            </div>
            <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'auth.verify' | t }}</button>
          </form>
          <app-otp-recovery [email]="emailForm.controls.email.value" [busy]="busy()" [seconds]="recovery.seconds()" (resend)="send()" (changeEmail)="changeEmail()" />
        }
        <p class="muted">{{ 'auth.noAccount' | t }} <a routerLink="/auth/register">{{ 'nav.register' | t }}</a></p>
      </div>
    </div>
  `,
})
export class ClientLoginComponent {
  private readonly i18n = inject(I18nService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  readonly emailForm = this.fb.nonNullable.group({
    email: ['', fieldRules.email],
  });
  readonly otpForm = this.fb.nonNullable.group({
    otp: ['', fieldRules.otp],
  });
  readonly otpSent = signal(false);
  readonly devOtp = signal<string | null>(null);
  readonly busy = signal(false);
  readonly recovery = new OtpRecoveryState();

  changeEmail(): void {
    if (this.busy()) return;
    this.otpSent.set(false);
    this.devOtp.set(null);
    this.otpForm.reset();
    this.emailForm.setErrors(null);
    this.recovery.focus('#email');
  }

  emailError(): string | null { return firstErrorKey(this.emailForm.controls.email); }
  otpError(): string | null { return firstErrorKey(this.otpForm.controls.otp); }

  send(): void {
    if (this.busy() || this.recovery.seconds() > 0) return;
    this.emailForm.setErrors(null);
    this.emailForm.markAllAsTouched();
    if (this.emailForm.invalid) return;
    this.busy.set(true);
    this.auth.clientLogin({ email: this.emailForm.controls.email.value }).subscribe({
      next: (res) => {
        this.recovery.start();
        this.otpForm.reset();
        this.otpSent.set(true);
        this.devOtp.set(res.developmentOtp ?? null);
        this.toast.success(res.message || this.i18n.t('auth.otpSent'));
        this.busy.set(false);
        this.recovery.focus('#otp');
      },
      error: (err) => { this.recovery.onError(err); applyServerError(this.otpSent() ? this.otpForm : this.emailForm, err); this.busy.set(false); },
    });
  }

  verify(): void {
    if (this.busy()) return;
    this.otpForm.markAllAsTouched();
    if (this.otpForm.invalid) return;
    this.busy.set(true);
    this.auth.clientVerifyLogin({ email: this.emailForm.controls.email.value, otp: this.otpForm.controls.otp.value }).subscribe({
      next: () => { this.toast.success(this.i18n.t('auth.welcome')); void this.router.navigateByUrl('/'); },
      error: (err) => { applyServerError(this.otpForm, err); this.busy.set(false); },
    });
  }
}
