import { I18nService } from '../../../core/services/i18n.service';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProfileDto } from '../../../core/models/api.models';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { firstErrorKey } from '../../../shared/forms/form-errors';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-client-profile',
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe],
  template: `
    <h1 class="page-title">{{ 'nav.profile' | t }}</h1>
    @if (profile()) {
      <form class="card stack" [formGroup]="form" (ngSubmit)="save()">
        <div class="field"><label for="client-profile-firstName">{{ 'auth.firstName' | t }}</label><input id="client-profile-firstName" formControlName="firstName" />
          @if (nameError('firstName'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="client-profile-lastName">{{ 'auth.lastName' | t }}</label><input id="client-profile-lastName" formControlName="lastName" />
          @if (nameError('lastName'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="client-profile-profile-email">{{ 'auth.email' | t }}</label><input id="client-profile-profile-email" [value]="profile()!.email" disabled /></div>
        <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'actions.save' | t }}</button>
      </form>
      <form class="card stack" style="margin-top:1rem" [formGroup]="emailForm" (ngSubmit)="requestEmail()">
        <h3>{{ 'profile.changeEmail' | t }}</h3>
        <div class="field"><label for="client-profile-newEmail">{{ 'profile.newEmail' | t }}</label><input id="client-profile-newEmail" type="email" formControlName="newEmail" />
          @if (emailError(); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        @if (otpSent()) {
          <div class="field"><label for="client-profile-otp">{{ 'auth.otp' | t }}</label><input id="client-profile-otp" formControlName="otp" inputmode="numeric" autocomplete="one-time-code" />
            @if (otpError(); as key) { <small class="field-error">{{ key | t }}</small> }</div>
          <button class="btn btn-secondary" type="button" [disabled]="busy()" (click)="confirmEmail()">{{ 'auth.verify' | t }}</button>
        } @else {
          <button class="btn btn-ghost" type="submit" [disabled]="busy()">{{ 'auth.sendOtp' | t }}</button>
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
  readonly form = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
  });
  readonly emailForm = this.fb.nonNullable.group({
    newEmail: ['', [Validators.required, Validators.email]],
    otp: [''],
  });

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
      error: () => this.busy.set(false),
    });
  }

  requestEmail(): void {
    this.emailForm.controls.newEmail.markAsTouched();
    if (this.emailForm.controls.newEmail.invalid) return;
    this.busy.set(true);
    this.auth.requestEmailChange('client', { newEmail: this.emailForm.controls.newEmail.value }).subscribe({
      next: (r) => {
        this.otpSent.set(true);
        this.emailForm.controls.otp.setValidators(Validators.required);
        this.emailForm.controls.otp.updateValueAndValidity();
        this.toast.success(r.message || this.i18n.t('auth.otpSent'));
        this.busy.set(false);
      },
      error: () => this.busy.set(false),
    });
  }

  confirmEmail(): void {
    this.emailForm.markAllAsTouched();
    if (this.emailForm.invalid) return;
    this.busy.set(true);
    this.auth.confirmEmailChange('client', this.emailForm.getRawValue()).subscribe({
      next: (p) => { this.profile.set(p); this.toast.success(this.i18n.t('profile.emailUpdated')); this.otpSent.set(false); this.busy.set(false); },
      error: () => this.busy.set(false),
    });
  }
}
