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
  selector: 'app-client-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, LogoComponent, TranslatePipe],
  template: `
    <div class="auth-shell">
      <div class="card auth-card stack">
        <app-logo />
        <h1 class="page-title">{{ 'nav.login' | t }}</h1>
        @if (!otpSent()) {
          <form [formGroup]="emailForm" (ngSubmit)="send()">
            <div class="field">
              <label for="email">{{ 'auth.email' | t }}</label>
              <input id="email" type="email" formControlName="email" autocomplete="email" />
              @if (emailError(); as key) { <small class="field-error">{{ key | t }}</small> }
            </div>
            <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'auth.sendOtp' | t }}</button>
          </form>
        } @else {
          <form [formGroup]="otpForm" (ngSubmit)="verify()">
            <p class="muted">{{ 'auth.codeSentTo' | t }} {{ emailForm.controls.email.value }}</p>
            @if (devOtp()) { <p class="badge">{{ 'auth.devOtp' | t }}: {{ devOtp() }}</p> }
            <div class="field">
              <label for="otp">{{ 'auth.otp' | t }}</label>
              <input id="otp" formControlName="otp" inputmode="numeric" autocomplete="one-time-code" />
              @if (otpError(); as key) { <small class="field-error">{{ key | t }}</small> }
            </div>
            <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'auth.verify' | t }}</button>
          </form>
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
    email: ['', [Validators.required, Validators.email]],
  });
  readonly otpForm = this.fb.nonNullable.group({
    otp: ['', Validators.required],
  });
  readonly otpSent = signal(false);
  readonly devOtp = signal<string | null>(null);
  readonly busy = signal(false);

  emailError(): string | null { return firstErrorKey(this.emailForm.controls.email); }
  otpError(): string | null { return firstErrorKey(this.otpForm.controls.otp); }

  send(): void {
    this.emailForm.markAllAsTouched();
    if (this.emailForm.invalid) return;
    this.busy.set(true);
    this.auth.clientLogin({ email: this.emailForm.controls.email.value }).subscribe({
      next: (res) => {
        this.otpSent.set(true);
        this.devOtp.set(res.developmentOtp ?? null);
        this.toast.success(res.message || this.i18n.t('auth.otpSent'));
        this.busy.set(false);
      },
      error: () => this.busy.set(false),
    });
  }

  verify(): void {
    this.otpForm.markAllAsTouched();
    if (this.otpForm.invalid) return;
    this.busy.set(true);
    this.auth.clientVerifyLogin({ email: this.emailForm.controls.email.value, otp: this.otpForm.controls.otp.value }).subscribe({
      next: () => { this.toast.success(this.i18n.t('auth.welcome')); void this.router.navigateByUrl('/'); },
      error: () => this.busy.set(false),
    });
  }
}
