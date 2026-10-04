import { Component, inject, input, output } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { VerificationPolicyService } from '../../../core/services/verification-policy.service';
import { I18nService } from '../../../core/services/i18n.service';
import { TranslatePipe } from '../../pipes/translate.pipe';

@Component({
  selector: 'app-otp-recovery',
  standalone: true,
  imports: [TranslatePipe, AsyncPipe],
  template: `
    <p class="muted" role="status">{{ 'auth.codeRequestedFor' | t }} <bdi>{{ email() }}</bdi></p>
    <p class="muted">{{ 'auth.codeHelp' | t }}</p>
    @if (policy$ | async; as policy) { <p class="muted">{{ expiryHint(policy.expirationMinutes, policy.maxAttempts) }}</p> }
    <div class="row">
      <button class="btn btn-ghost" type="button" [disabled]="busy() || seconds() > 0" (click)="resend.emit()">
        {{ 'auth.resendCode' | t }}
        @if (seconds() > 0) { <span>({{ seconds() }} {{ 'auth.secondsShort' | t }})</span> }
      </button>
      <button class="btn btn-ghost" type="button" [disabled]="busy()" (click)="changeEmail.emit()">
        {{ 'auth.useAnotherEmail' | t }}
      </button>
    </div>
  `,
})
export class OtpRecoveryComponent {
  readonly policy$ = inject(VerificationPolicyService).policy$;
  private readonly i18n = inject(I18nService);
  expiryHint(minutes: number, attempts: number): string {
    return this.i18n.t('auth.codeExpiryHint').replace('{minutes}', String(minutes)).replace('{attempts}', String(attempts));
  }
  readonly email = input.required<string>();
  readonly busy = input(false);
  readonly seconds = input(0);
  readonly resend = output<void>();
  readonly changeEmail = output<void>();
}
