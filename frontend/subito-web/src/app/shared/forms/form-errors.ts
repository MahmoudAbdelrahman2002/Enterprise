import { AbstractControl, FormGroup, ValidationErrors } from '@angular/forms';

export function strongPassword(control: AbstractControl): ValidationErrors | null {
  const value = String(control.value ?? '');
  if (!value) return null;
  if (value.length < 8) return { minlength: { requiredLength: 8, actualLength: value.length } };
  if (!/[A-Z]/.test(value)) return { passwordUpper: true };
  if (!/[a-z]/.test(value)) return { passwordLower: true };
  if (!/\d/.test(value)) return { passwordDigit: true };
  if (!/[^A-Za-z0-9]/.test(value)) return { passwordSpecial: true };
  return null;
}

export function firstErrorKey(control: AbstractControl | null): string | null {
  if (!control || !control.touched || !control.errors) return null;
  if (control.errors['required']) return 'validation.required';
  if (control.errors['email']) return 'validation.email';
  if (control.errors['minlength'] || control.errors['passwordUpper'] || control.errors['passwordLower'] || control.errors['passwordDigit'] || control.errors['passwordSpecial']) {
    return 'validation.password';
  }
  if (control.errors['min']) return 'validation.min';
  if (control.errors['server']) return 'validation.server';
  return 'validation.required';
}

export function applyServerError(form: FormGroup, err: unknown): void {
  const message = (err as { message?: string })?.message;
  if (!message) return;
  form.setErrors({ server: message });
}
