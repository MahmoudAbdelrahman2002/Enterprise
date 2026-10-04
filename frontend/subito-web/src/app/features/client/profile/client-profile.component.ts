import { FieldValidationDirective } from '../../../shared/directives/field-validation.directive';
import { fieldRules } from '../../../shared/forms/field-validators';
import { I18nService } from '../../../core/services/i18n.service';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProfileDto } from '../../../core/models/api.models';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { applyServerError, firstErrorKey } from '../../../shared/forms/form-errors';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { OtpRecoveryComponent } from '../../../shared/components/otp-recovery/otp-recovery.component';
import { OtpRecoveryState } from '../../../shared/forms/otp-recovery';

@Component({
  selector: 'app-client-profile',
  standalone: true,
  imports: [OtpRecoveryComponent, FieldValidationDirective, ReactiveFormsModule, TranslatePipe],
  template: `
    <h1 class="page-title">{{ 'nav.profile' | t }}</h1>
    @if (profile()) {
      <form class="card stack" [formGroup]="form" (ngSubmit)="save()">
        @if (form.errors?.['server']) { <p class="field-error" role="alert">{{ form.errors?.['server'] }}</p> }
        <div class="field"><label for="client-profile-firstName">{{ 'auth.firstName' | t }}</label><input id="client-profile-firstName" appFieldValidation formControlName="firstName" /></div>
        <div class="field"><label for="client-profile-lastName">{{ 'auth.lastName' | t }}</label><input id="client-profile-lastName" appFieldValidation formControlName="lastName" /></div>
        <div class="field"><label for="client-profile-profile-email">{{ 'auth.email' | t }}</label><input id="client-profile-profile-email" [value]="profile()!.email" disabled /></div>
        <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'actions.save' | t }}</button>
      </form>
      <form class="card stack" style="margin-top:1rem" [formGroup]="emailForm" (ngSubmit)="otpSent() ? confirmEmail() : requestEmail()">
        @if (emailForm.errors?.['server']) { <p class="field-error" role="alert">{{ emailForm.errors?.['server'] }}</p> }
        <h3>{{ 'profile.changeEmail' | t }}</h3>
        <div class="field"><label for="client-profile-newEmail">{{ 'profile.newEmail' | t }}</label><input id="client-profile-newEmail" type="email" [readOnly]="otpSent() || busy()" appFieldValidation formControlName="newEmail" autocomplete="email" /></div>
        @if (otpSent()) {
          <div class="field"><label for="client-profile-otp">{{ 'auth.otp' | t }}</label><input id="client-profile-otp" appFieldValidation formControlName="otp" inputmode="numeric" autocomplete="one-time-code" /></div>
          <button class="btn btn-secondary" type="submit" [disabled]="busy()">{{ 'auth.verify' | t }}</button>
          <app-otp-recovery [email]="emailForm.controls.newEmail.value" [busy]="busy()" [seconds]="recovery.seconds()" (resend)="requestEmail()" (changeEmail)="changeEmail()" />
        } @else {
          <button class="btn btn-ghost" type="submit" [disabled]="busy() || recovery.seconds() > 0">{{ 'auth.sendOtp' | t }} @if (recovery.seconds() > 0) { ({{ recovery.seconds() }} {{ 'auth.secondsShort' | t }}) }</button>
        }
      </form>
    }
  `,
})
export class ClientProfileComponent implements OnInit {
  private readonly i18n = inject(I18nService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  readonly profile = signal<ProfileDto | null>(null);
  readonly otpSent = signal(false);
  readonly busy = signal(false);
  readonly recovery = new OtpRecoveryState();
  readonly form = this.fb.nonNullable.group({
    firstName: ['', fieldRules.name],
    lastName: ['', fieldRules.name],
  });
  readonly emailForm = this.fb.nonNullable.group({
    newEmail: ['', fieldRules.email],
    otp: [''],
  });

  changeEmail(): void {
    if (this.busy()) return;
    this.otpSent.set(false);
    this.emailForm.controls.otp.clearValidators();
    this.emailForm.controls.otp.reset();
    this.emailForm.setErrors(null);
    this.recovery.focus('#client-profile-newEmail');
  }

  ngOnInit(): void {
    this.auth.getProfile('client').subscribe({
      next: (p) => { this.profile.set(p); this.form.patchValue({ firstName: p.firstName, lastName: p.lastName }); },
    });
  }

  nameError(name: 'firstName' | 'lastName'): string | null { return firstErrorKey(this.form.controls[name]); }
  emailError(): string | null { return firstErrorKey(this.emailForm.controls.newEmail); }
  otpError(): string | null { return firstErrorKey(this.emailForm.controls.otp); }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.busy.set(true);
    this.auth.updateProfile('client', this.form.getRawValue()).subscribe({
      next: (p) => { this.profile.set(p); this.toast.success(this.i18n.t('ui.saved')); this.busy.set(false); },
      error: (err) => { applyServerError(this.form, err); this.busy.set(false); },
    });
  }

  requestEmail(): void {
    if (this.busy() || this.recovery.seconds() > 0) return;
    this.emailForm.setErrors(null);
    this.emailForm.controls.newEmail.markAsTouched();
    if (this.emailForm.controls.newEmail.invalid) return;
    this.busy.set(true);
    this.auth.requestEmailChange('client', { newEmail: this.emailForm.controls.newEmail.value }).subscribe({
      next: (r) => {
        this.recovery.start();
        this.emailForm.controls.otp.reset();
        this.otpSent.set(true);
        this.emailForm.controls.otp.setValidators(fieldRules.otp);
        this.emailForm.controls.otp.updateValueAndValidity();
        this.toast.success(r.message || this.i18n.t('auth.otpSent'));
        this.busy.set(false);
        this.recovery.focus('#client-profile-otp');
      },
      error: (err) => { this.recovery.onError(err); applyServerError(this.emailForm, err); this.busy.set(false); },
    });
  }

  confirmEmail(): void {
    if (this.busy()) return;
    this.emailForm.markAllAsTouched();
    if (this.emailForm.invalid) return;
    this.busy.set(true);
    this.auth.confirmEmailChange('client', this.emailForm.getRawValue()).subscribe({
      next: (p) => {
        this.profile.set(p); this.toast.success(this.i18n.t('profile.emailUpdated')); this.busy.set(false);
        this.changeEmail(); this.emailForm.controls.newEmail.reset();
      },
      error: (err) => { applyServerError(this.emailForm, err); this.busy.set(false); },
    });
  }
}
