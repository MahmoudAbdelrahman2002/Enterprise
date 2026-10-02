import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { AdminLoginComponent } from './admin-login.component';

describe('AdminLoginComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminLoginComponent],
      providers: [provideRouter([]), provideHttpClient()],
    }).compileComponents();
  });

  it('keeps an empty login form invalid and marks the fields', () => {
    const fixture = TestBed.createComponent(AdminLoginComponent);
    const component = fixture.componentInstance;
    component.login();
    expect(component.form.invalid).toBeTrue();
    expect(component.form.controls.email.hasError('required')).toBeTrue();
    expect(component.form.controls.password.touched).toBeTrue();
    expect(component.busy()).toBeFalse();
  });

  it('rejects a malformed email', () => {
    const fixture = TestBed.createComponent(AdminLoginComponent);
    const component = fixture.componentInstance;
    component.form.setValue({ email: 'not-an-email', password: 'secret' });
    component.login();
    expect(component.form.controls.email.hasError('email')).toBeTrue();
    expect(component.busy()).toBeFalse();
  });
});
