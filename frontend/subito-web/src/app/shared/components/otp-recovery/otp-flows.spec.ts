import { Type, signal } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { AbstractControl, FormGroup } from '@angular/forms';
import { provideRouter } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { I18nService } from '../../../core/services/i18n.service';
import { ToastService } from '../../../core/services/toast.service';
import { OtpSentDto } from '../../../core/models/api.models';
import { ClientLoginComponent } from '../../../features/client/auth/client-login.component';
import { ClientRegisterComponent } from '../../../features/client/auth/client-register.component';
import { ClientProfileComponent } from '../../../features/client/profile/client-profile.component';
import { AdminForgotComponent } from '../../../features/admin/auth/admin-forgot.component';
import { ProviderForgotComponent } from '../../../features/provider/auth/provider-forgot.component';
import { OtpRecoveryState } from '../../forms/otp-recovery';
import { VerificationPolicyService } from '../../../core/services/verification-policy.service';

describe('Code-entry recovery across portals', () => {
  const destination = 'person@example.com';
  const response: OtpSentDto = { message: 'Code requested', developmentOtp: '123456' };
  const flows: { name: string; type: Type<unknown>; method: 'clientLogin' | 'forgotPassword' | 'requestEmailChange' }[] = [
    { name: 'client login', type: ClientLoginComponent, method: 'clientLogin' },
    { name: 'client registration', type: ClientRegisterComponent, method: 'clientLogin' },
    { name: 'client email change', type: ClientProfileComponent, method: 'requestEmailChange' },
    { name: 'admin recovery', type: AdminForgotComponent, method: 'forgotPassword' },
    { name: 'provider recovery', type: ProviderForgotComponent, method: 'forgotPassword' },
  ];
  let auth: jasmine.SpyObj<AuthService>;
  let fixture: ComponentFixture<unknown>;
  let page: HTMLElement;
  let send: () => void;
  let form: FormGroup;
  let code: AbstractControl;
  let recovery: OtpRecoveryState;
  let emailControl: AbstractControl;

  for (const flow of flows) {
    describe(flow.name, () => {
      beforeEach(async () => {
        auth = jasmine.createSpyObj<AuthService>('AuthService', [
          'clientRegister', 'clientLogin', 'forgotPassword', 'requestEmailChange', 'getProfile',
          'clientVerifyRegistration', 'clientVerifyLogin', 'resetPassword', 'confirmEmailChange', 'updateProfile',
        ]);
        auth.clientRegister.and.returnValue(of(response));
        auth.clientLogin.and.returnValue(of(response));
        auth.forgotPassword.and.returnValue(of(response));
        auth.requestEmailChange.and.returnValue(of(response));
        auth.getProfile.and.returnValue(of({ id: 'client', firstName: 'First', lastName: 'Last', email: 'old@example.com', userType: 'Client', emailConfirmed: true }));
        await TestBed.configureTestingModule({
          imports: [flow.type],
          providers: [
            provideRouter([]),
            { provide: VerificationPolicyService, useValue: { policy$: of({ codeLength: 6, expirationMinutes: 10, maxAttempts: 5, resendWaitSeconds: 30 }) } },
            { provide: AuthService, useValue: auth },
            { provide: ToastService, useValue: { success: () => {} } },
            { provide: I18nService, useValue: { lang: signal('en'), t: (key: string) => key } },
          ],
        }).compileComponents();
        fixture = TestBed.createComponent(flow.type);
        const component = fixture.componentInstance;
        fixture.detectChanges();
        page = fixture.nativeElement;
        if (component instanceof ClientProfileComponent) {
          form = component.emailForm;
          emailControl = form.controls['newEmail'];
          send = () => component.requestEmail();
          code = form.controls['otp'];
          recovery = component.recovery;
        } else if (component instanceof ClientRegisterComponent) {
          form = component.form;
          form.patchValue({ firstName: 'First', lastName: 'Last' });
          emailControl = form.controls['email'];
          send = () => component.send();
          code = component.otpForm.controls.otp;
          recovery = component.recovery;
        } else if (component instanceof ClientLoginComponent) {
          form = component.emailForm;
          emailControl = form.controls['email'];
          send = () => component.send();
          code = component.otpForm.controls.otp;
          recovery = component.recovery;
        } else if (component instanceof AdminForgotComponent || component instanceof ProviderForgotComponent) {
          form = component.emailForm;
          emailControl = form.controls['email'];
          send = () => component.send();
          code = component.resetForm.controls.otp;
          component.resetForm.controls.newPassword.setValue('PreservedPassword1!');
          recovery = component.recovery;
        } else throw new Error('Unsupported flow');
        emailControl.setValue(destination);
      });

      afterEach(() => fixture.destroy());

      function controls(): HTMLButtonElement[] {
        return Array.from(page.querySelectorAll<HTMLButtonElement>('app-otp-recovery button'));
      }

      it('shows the destination and allows resend after exactly 30 seconds', fakeAsync(() => {
        const now = spyOn(Date, 'now').and.returnValue(0);
        expect(controls().length).toBe(0);
        send(); fixture.detectChanges();
        expect(page.querySelector('app-otp-recovery [role="status"]')!.textContent).toContain(destination);
        expect(controls()[0].disabled).toBeTrue();
        const requests = auth[flow.method].calls.count();
        send();
        expect(auth[flow.method].calls.count()).toBe(requests);
        now.and.returnValue(29000); tick(29000); fixture.detectChanges();
        expect(controls()[0].disabled).toBeTrue();
        now.and.returnValue(30000); tick(1000); fixture.detectChanges();
        expect(controls()[0].disabled).toBeFalse();
        code.setValue('999999');
        controls()[0].click(); fixture.detectChanges();
        expect(auth[flow.method].calls.count()).toBe(requests + 1);
        expect(code.value).toBe('');
        expect(recovery.seconds()).toBe(30);
        if (fixture.componentInstance instanceof ClientRegisterComponent) {
          expect(auth.clientRegister).toHaveBeenCalledTimes(1);
          expect(auth.clientLogin).toHaveBeenCalledWith({ email: destination });
        }
      }));

      it('returns to edit email without losing other details or bypassing cooldown', fakeAsync(() => {
        const now = spyOn(Date, 'now').and.returnValue(0);
        send(); fixture.detectChanges();
        code.setValue('999999');
        controls()[1].click(); fixture.detectChanges();
        expect(controls().length).toBe(0);
        expect(document.activeElement).toBe(page.querySelector('input[type="email"]:not(:disabled)'));
        expect(emailControl.value).toBe(destination);
        expect(code.value).toBe('');
        expect(form.errors).toBeNull();
        const requests = auth[flow.method].calls.count();
        send();
        expect(auth[flow.method].calls.count()).toBe(requests);
        const component = fixture.componentInstance;
        if (component instanceof ClientRegisterComponent) {
          expect(component.form.controls.firstName.value).toBe('First');
          expect(component.form.controls.lastName.value).toBe('Last');
        }
        if (component instanceof AdminForgotComponent || component instanceof ProviderForgotComponent) {
          expect(component.resetForm.controls.newPassword.value).toBe('PreservedPassword1!');
        }
        if (component instanceof ClientProfileComponent) {
          expect((page.querySelector('#client-profile-newEmail') as HTMLInputElement).readOnly).toBeFalse();
          expect(code.valid).toBeTrue();
        }
        now.and.returnValue(30000); tick(30000);
        emailControl.setValue('corrected@example.com');
        send(); fixture.detectChanges();
        expect(page.querySelector('app-otp-recovery [role="status"]')!.textContent).toContain('corrected@example.com');
      }));

      it('keeps failed resends visible at the code stage and retains entered code', fakeAsync(() => {
        const now = spyOn(Date, 'now').and.returnValue(0);
        send(); fixture.detectChanges(); now.and.returnValue(30000); tick(30000);
        code.setValue('999999');
        auth[flow.method].and.returnValue(throwError(() => ({ message: 'Delivery failed', statusCode: 503 })));
        send(); fixture.detectChanges();
        expect(page.querySelector('[role="alert"]')!.textContent).toContain('Delivery failed');
        expect(controls().length).toBe(2);
        expect(controls()[0].disabled).toBeFalse();
        expect(code.value).toBe('999999');
        auth[flow.method].and.returnValue(of(response));
        controls()[0].click(); fixture.detectChanges();
        expect(page.querySelector('[role="alert"]')).toBeNull();
      }));

      it('prevents duplicate requests while sending and waits after server throttling', fakeAsync(() => {
        const now = spyOn(Date, 'now').and.returnValue(0);
        send(); fixture.detectChanges(); now.and.returnValue(30000); tick(30000);
        const pending = new Subject<OtpSentDto>();
        auth[flow.method].and.returnValue(pending);
        send(); fixture.detectChanges();
        const count = auth[flow.method].calls.count();
        send();
        expect(auth[flow.method].calls.count()).toBe(count);
        expect(controls().every((button) => button.disabled)).toBeTrue();
        pending.error({ statusCode: 429, message: 'Please wait before trying again' });
        fixture.detectChanges();
        expect(recovery.seconds()).toBe(60);
        expect(page.querySelector('[role="alert"]')!.textContent).toContain('Please wait');
        now.and.returnValue(89000); tick(59000); fixture.detectChanges();
        expect(controls()[0].disabled).toBeTrue();
        now.and.returnValue(90000); tick(1000); fixture.detectChanges();
        expect(controls()[0].disabled).toBeFalse();
      }));

      if (flow.type === ClientProfileComponent) {
        it('submits email verification from the code form and clears obsolete code validation', fakeAsync(() => {
          send(); fixture.detectChanges();
          expect((page.querySelector('#client-profile-newEmail') as HTMLInputElement).readOnly).toBeTrue();
          auth.confirmEmailChange.and.returnValue(of({
            id: 'client', firstName: 'First', lastName: 'Last', email: destination,
            userType: 'Client', emailConfirmed: true,
          }));
          code.setValue('123456');
          page.querySelectorAll('form')[1].dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
          fixture.detectChanges();
          expect(auth.confirmEmailChange).toHaveBeenCalledWith('client', { newEmail: destination, otp: '123456' });
          expect(controls().length).toBe(0);
          expect(code.valid).toBeTrue();
          expect(code.value).toBe('');
          expect(emailControl.value).toBe('');
        }));
      }

      if (flow.type === AdminForgotComponent || flow.type === ProviderForgotComponent) {
        it('enables locally valid reset submission and unlocks after server rejection', fakeAsync(() => {
          send(); fixture.detectChanges();
          const component = fixture.componentInstance as AdminForgotComponent | ProviderForgotComponent;
          component.resetForm.setValue({ otp: '123456', newPassword: 'ValidPassword1!' });
          fixture.detectChanges();
          const submit = page.querySelector<HTMLButtonElement>('form button[type="submit"]')!;
          expect(component.resetForm.valid).toBeTrue();
          expect(submit.disabled).toBeFalse();
          const pending = new Subject<unknown>();
          auth.resetPassword.and.returnValue(pending);
          page.querySelector('form')!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
          fixture.detectChanges();
          expect(auth.resetPassword).toHaveBeenCalledWith(flow.type === AdminForgotComponent ? 'admin' : 'provider', {
            email: destination, otp: '123456', newPassword: 'ValidPassword1!',
          });
          expect(submit.disabled).toBeTrue();
          pending.error({ message: 'The code is invalid or expired' });
          fixture.detectChanges();
          expect(submit.disabled).toBeFalse();
          expect(page.querySelector('[role="alert"]')!.textContent).toContain('invalid or expired');
          expect(component.resetForm.controls.newPassword.value).toBe('ValidPassword1!');
        }));
      }
    });
  }
});
