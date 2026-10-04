import { FieldValidationDirective } from '../../../shared/directives/field-validation.directive';
import { fieldRules, differentPasswords } from '../../../shared/forms/field-validators';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { PasswordToggleDirective } from '../../../shared/directives/password-toggle.directive';
import { I18nService } from '../../../core/services/i18n.service';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProfileDto } from '../../../core/models/api.models';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { applyServerError, firstErrorKey, strongPassword } from '../../../shared/forms/form-errors';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-provider-profile',
  standalone: true,
  imports: [FieldValidationDirective, IconComponent, PasswordToggleDirective, ReactiveFormsModule, TranslatePipe],
  template: `
    <h1 class="page-title">{{ 'nav.profile' | t }}</h1>
    @if (profile()) {
      <form class="card stack" [formGroup]="form" (ngSubmit)="save()">
        @if (form.errors?.['server']) { <p class="field-error" role="alert">{{ form.errors?.['server'] }}</p> }
        <div class="field"><label for="provider-profile-firstName">{{ 'auth.firstName' | t }}</label><input id="provider-profile-firstName" appFieldValidation formControlName="firstName" /></div>
        <div class="field"><label for="provider-profile-lastName">{{ 'auth.lastName' | t }}</label><input id="provider-profile-lastName" appFieldValidation formControlName="lastName" /></div>
        <div class="field"><label for="provider-profile-profile-email">{{ 'auth.email' | t }}</label><input id="provider-profile-profile-email" [value]="profile()!.email" disabled /></div>
        <button class="btn btn-primary icon-action" type="submit" [disabled]="busy()" [attr.aria-label]="'actions.save' | t" [title]="'actions.save' | t"><app-icon name="check" />{{ 'actions.save' | t }}</button>
      </form>
      <form class="card stack" style="margin-top:1rem" [formGroup]="passwordForm" (ngSubmit)="changePassword()">
        @if (passwordForm.errors?.['server']) { <p class="field-error" role="alert">{{ passwordForm.errors?.['server'] }}</p> }
        <h3>{{ 'auth.password' | t }}</h3>
        <div class="field"><label for="provider-profile-currentPassword">{{ 'auth.currentPassword' | t }}</label><input id="provider-profile-currentPassword" type="password" appPasswordToggle appFieldValidation formControlName="currentPassword" /></div>
        <div class="field"><label for="provider-profile-newPassword">{{ 'auth.newPassword' | t }}</label><input id="provider-profile-newPassword" type="password" appPasswordToggle appFieldValidation formControlName="newPassword" autocomplete="new-password" /></div>
        <button class="btn btn-secondary icon-action" type="submit" [disabled]="busy()" [attr.aria-label]="'actions.save' | t" [title]="'actions.save' | t"><app-icon name="check" />{{ 'actions.save' | t }}</button>
      </form>
    }
  `,
})
export class ProviderProfileComponent implements OnInit {
  private readonly i18n = inject(I18nService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  readonly profile = signal<ProfileDto | null>(null);
  readonly busy = signal(false);
  readonly form = this.fb.nonNullable.group({
    firstName: ['', fieldRules.name],
    lastName: ['', fieldRules.name],
  });
  readonly passwordForm = this.fb.nonNullable.group({
    currentPassword: ['', fieldRules.existingPassword],
    newPassword: ['', fieldRules.password],
  }, { validators: differentPasswords });

  ngOnInit(): void {
    this.auth.getProfile('provider').subscribe({
      next: (p) => { this.profile.set(p); this.form.patchValue({ firstName: p.firstName, lastName: p.lastName }); },
    });
  }

  nameError(name: 'firstName' | 'lastName'): string | null { return firstErrorKey(this.form.controls[name]); }
  currentError(): string | null { return firstErrorKey(this.passwordForm.controls.currentPassword); }
  newError(): string | null { return firstErrorKey(this.passwordForm.controls.newPassword); }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.busy.set(true);
    this.auth.updateProfile('provider', this.form.getRawValue()).subscribe({
      next: (p) => { this.profile.set(p); this.toast.success(this.i18n.t('ui.saved')); this.busy.set(false); },
      error: (err) => { applyServerError(this.form, err); this.busy.set(false); },
    });
  }

  changePassword(): void {
    this.passwordForm.markAllAsTouched();
    if (this.passwordForm.invalid) return;
    this.busy.set(true);
    this.auth.changePassword('provider', this.passwordForm.getRawValue()).subscribe({
      next: () => { this.toast.success(this.i18n.t('auth.passwordUpdated')); this.passwordForm.reset(); this.busy.set(false); },
      error: (err) => { applyServerError(this.passwordForm, err); this.busy.set(false); },
    });
  }
}
