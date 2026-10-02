import { I18nService } from '../../../core/services/i18n.service';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProfileDto } from '../../../core/models/api.models';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { firstErrorKey, strongPassword } from '../../../shared/forms/form-errors';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-profile',
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe],
  template: `
    <h1 class="page-title">{{ 'nav.profile' | t }}</h1>
    @if (profile()) {
      <form class="card stack" [formGroup]="form" (ngSubmit)="save()">
        <div class="field"><label for="admin-profile-firstName">{{ 'auth.firstName' | t }}</label><input id="admin-profile-firstName" formControlName="firstName" />
          @if (nameError('firstName'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="admin-profile-lastName">{{ 'auth.lastName' | t }}</label><input id="admin-profile-lastName" formControlName="lastName" />
          @if (nameError('lastName'); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="admin-profile-profile-email">{{ 'auth.email' | t }}</label><input id="admin-profile-profile-email" [value]="profile()!.email" disabled /></div>
        <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'actions.save' | t }}</button>
      </form>
      <form class="card stack" style="margin-top:1rem" [formGroup]="passwordForm" (ngSubmit)="changePassword()">
        <h3>{{ 'auth.password' | t }}</h3>
        <div class="field"><label for="admin-profile-currentPassword">{{ 'auth.currentPassword' | t }}</label><input id="admin-profile-currentPassword" type="password" formControlName="currentPassword" />
          @if (currentError(); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <div class="field"><label for="admin-profile-newPassword">{{ 'auth.newPassword' | t }}</label><input id="admin-profile-newPassword" type="password" formControlName="newPassword" autocomplete="new-password" />
          @if (newError(); as key) { <small class="field-error">{{ key | t }}</small> }</div>
        <button class="btn btn-secondary" type="submit" [disabled]="busy()">{{ 'actions.save' | t }}</button>
      </form>
    }
  `,
})
export class AdminProfileComponent implements OnInit {
  private readonly i18n = inject(I18nService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  readonly profile = signal<ProfileDto | null>(null);
  readonly busy = signal(false);
  readonly form = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
  });
  readonly passwordForm = this.fb.nonNullable.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, strongPassword]],
  });

  ngOnInit(): void {
    this.auth.getProfile('admin').subscribe({
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
    this.auth.updateProfile('admin', this.form.getRawValue()).subscribe({
      next: (p) => { this.profile.set(p); this.toast.success(this.i18n.t('ui.saved')); this.busy.set(false); },
      error: () => this.busy.set(false),
    });
  }

  changePassword(): void {
    this.passwordForm.markAllAsTouched();
    if (this.passwordForm.invalid) return;
    this.busy.set(true);
    this.auth.changePassword('admin', this.passwordForm.getRawValue()).subscribe({
      next: () => { this.toast.success(this.i18n.t('auth.passwordUpdated')); this.passwordForm.reset(); this.busy.set(false); },
      error: () => this.busy.set(false),
    });
  }
}
