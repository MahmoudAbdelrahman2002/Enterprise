import { I18nService } from '../../../core/services/i18n.service';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { firstErrorKey, strongPassword } from '../../../shared/forms/form-errors';
import { LogoComponent } from '../../../shared/components/logo/logo.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-forgot',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, LogoComponent, TranslatePipe],
  template: `
    <div class="auth-shell"><div class="card auth-card stack">
      <app-logo /><h1 class="page-title">{{ 'auth.forgot' | t }}</h1>
      @if (!sent()) {
        <form [formGroup]="emailForm" (ngSubmit)="send()">
          <div class="field"><label for="admin-forgot-email">{{ 'auth.email' | t }}</label><input id="admin-forgot-email" type="email" formControlName="email" autocomplete="email" />
            @if (emailError(); as key) { <small class="field-error">{{ key | t }}</small> }</div>
          <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'auth.sendOtp' | t }}</button>
        </form>
      } @else {
        <form [formGroup]="resetForm" (ngSubmit)="reset()">
          @if (devOtp()) { <p class="badge">{{ 'auth.devOtp' | t }}: {{ devOtp() }}</p> }
          <div class="field"><label for="admin-forgot-otp">{{ 'auth.otp' | t }}</label><input id="admin-forgot-otp" formControlName="otp" inputmode="numeric" autocomplete="one-time-code" />
            @if (otpError(); as key) { <small class="field-error">{{ key | t }}</small> }</div>
          <div class="field"><label for="admin-forgot-newPassword">{{ 'auth.newPassword' | t }}</label><input id="admin-forgot-newPassword" type="password" formControlName="newPassword" autocomplete="new-password" />
            @if (passwordError(); as key) { <small class="field-error">{{ key | t }}</small> }</div>
          <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'auth.reset' | t }}</button>
        </form>
      }
      <a routerLink="/admin/login">{{ 'nav.login' | t }}</a>
    </div></div>
  `,
})
export class AdminForgotComponent {
  private readonly i18n = inject(I18nService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  readonly emailForm = this.fb.nonNullable.group({ email: ['', [Validators.required, Validators.email]] });
  readonly resetForm = this.fb.nonNullable.group({
    otp: ['', Validators.required],
    newPassword: ['', [Validators.required, strongPassword]],
  });
  readonly sent = signal(false);
  readonly busy = signal(false);
  readonly devOtp = signal<string | null>(null);

  emailError(): string | null { return firstErrorKey(this.emailForm.controls.email); }
  otpError(): string | null { return firstErrorKey(this.resetForm.controls.otp); }
  passwordError(): string | null { return firstErrorKey(this.resetForm.controls.newPassword); }

  send(): void {
    this.emailForm.markAllAsTouched();
    if (this.emailForm.invalid) return;
    this.busy.set(true);
    this.auth.forgotPassword('admin', { email: this.emailForm.controls.email.value }).subscribe({
      next: (r) => { this.sent.set(true); this.devOtp.set(r.developmentOtp ?? null); this.toast.success(r.message || this.i18n.t('auth.otpSent')); this.busy.set(false); },
      error: () => this.busy.set(false),
    });
  }

  reset(): void {
    this.resetForm.markAllAsTouched();
    if (this.resetForm.invalid) return;
    this.busy.set(true);
    this.auth.resetPassword('admin', {
      email: this.emailForm.controls.email.value,
      otp: this.resetForm.controls.otp.value,
      newPassword: this.resetForm.controls.newPassword.value,
    }).subscribe({
      next: () => { this.toast.success(this.i18n.t('auth.passwordUpdated')); this.busy.set(false); },
      error: () => this.busy.set(false),
    });
  }
}
