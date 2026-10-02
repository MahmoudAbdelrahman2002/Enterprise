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
  selector: 'app-provider-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, LogoComponent, TranslatePipe],
  template: `
    <div class="auth-shell">
      <div class="card auth-card stack">
        <app-logo link="/provider" />
        <h1 class="page-title">{{ 'nav.login' | t }}</h1>
        <form [formGroup]="form" (ngSubmit)="login()">
          <div class="field">
            <label for="email">{{ 'auth.email' | t }}</label>
            <input id="email" type="email" formControlName="email" autocomplete="email" />
            @if (error('email'); as key) { <small class="field-error">{{ key | t }}</small> }
          </div>
          <div class="field">
            <label for="password">{{ 'auth.password' | t }}</label>
            <input id="password" type="password" formControlName="password" autocomplete="current-password" />
            @if (error('password'); as key) { <small class="field-error">{{ key | t }}</small> }
          </div>
          <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'nav.login' | t }}</button>
        </form>
        <a routerLink="/provider/forgot-password">{{ 'auth.forgot' | t }}</a>
      </div>
    </div>
  `,
})
export class ProviderLoginComponent {
  private readonly i18n = inject(I18nService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });
  readonly busy = signal(false);

  error(name: 'email' | 'password'): string | null {
    return firstErrorKey(this.form.controls[name]);
  }

  login(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.busy.set(true);
    const { email, password } = this.form.getRawValue();
    this.auth.passwordLogin('provider', { email, password }).subscribe({
      next: () => { this.toast.success(this.i18n.t('auth.welcome')); void this.router.navigateByUrl('/provider'); },
      error: () => this.busy.set(false),
    });
  }
}
