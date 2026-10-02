import { I18nService } from '../../../core/services/i18n.service';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { firstErrorKey } from '../../../shared/forms/form-errors';
import { LogoComponent } from '../../../shared/components/logo/logo.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-client-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, LogoComponent, TranslatePipe],
  template: `
    <div class="auth-shell">
      <div class="card auth-card stack">
        <app-logo />
        <h1 class="page-title">{{ 'nav.register' | t }}</h1>
        @if (!otpSent()) {
          <form [formGroup]="form" (ngSubmit)="send()">
            <div class="field"><label for="client-register-firstName">{{ 'auth.firstName' | t }}</label><input id="client-register-firstName" formControlName="firstName" />
              @if (error('firstName'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
            <div class="field"><label for="client-register-lastName">{{ 'auth.lastName' | t }}</label><input id="client-register-lastName" formControlName="lastName" />
              @if (error('lastName'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
            <div class="field"><label for="client-register-email">{{ 'auth.email' | t }}</label><input id="client-register-email" type="email" formControlName="email" autocomplete="email" />
              @if (error('email'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
            <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'auth.sendOtp' | t }}</button>
          </form>
        } @else {
          <form [formGroup]="otpForm" (ngSubmit)="verify()">
            @if (devOtp()) { <p class="badge">{{ 'auth.devOtp' | t }}: {{ devOtp() }}</p> }
            <div class="field"><label for="client-register-otp">{{ 'auth.otp' | t }}</label><input id="client-register-otp" formControlName="otp" inputmode="numeric" autocomplete="one-time-code" />
              @if (otpError(); as key) { <small class="field-error">{{ key | t }}</small> }</div>
            <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'auth.verify' | t }}</button>
          </form>
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
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
  });
  readonly otpForm = this.fb.nonNullable.group({ otp: ['', Validators.required] });
  readonly otpSent = signal(false);
  readonly devOtp = signal<string | null>(null);
  readonly busy = signal(false);

  error(name: 'firstName' | 'lastName' | 'email'): string | null {
    return firstErrorKey(this.form.controls[name]);
  }
  otpError(): string | null { return firstErrorKey(this.otpForm.controls.otp); }

  send(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.busy.set(true);
    this.auth.clientRegister(this.form.getRawValue()).subscribe({
      next: (res) => { this.otpSent.set(true); this.devOtp.set(res.developmentOtp ?? null); this.toast.success(res.message || this.i18n.t('auth.otpSent')); this.busy.set(false); },
      error: () => this.busy.set(false),
    });
  }

  verify(): void {
    this.otpForm.markAllAsTouched();
    if (this.otpForm.invalid) return;
    this.busy.set(true);
    this.auth.clientVerifyRegistration({ email: this.form.controls.email.value, otp: this.otpForm.controls.otp.value }).subscribe({
      next: () => { this.toast.success(this.i18n.t('auth.created')); void this.router.navigateByUrl('/'); },
      error: () => this.busy.set(false),
    });
  }
}
