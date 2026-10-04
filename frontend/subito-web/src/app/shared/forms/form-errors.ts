import { AbstractControl, FormGroup, Validators } from '@angular/forms';
export { strongPassword } from './field-validators';

export function firstErrorKey(control: AbstractControl | null): string | null {
  if (!control || !control.touched || !control.errors) return null;
  const errors = control.errors;
  if (typeof control.value === 'string' && !control.value.trim() && control.hasValidator(Validators.required)) return 'validation.required';
  if (errors['server']) return 'validation.server';
  if (errors['required']) return 'validation.required';
  if (errors['email']) return 'validation.email';
  if (errors['maxlength']) return 'validation.maxLength';
  if (errors['minlength']) return 'validation.minLength';
  if (errors['passwordStrength'] || errors['passwordUpper'] || errors['passwordLower'] || errors['passwordDigit'] || errors['passwordSpecial']) return 'validation.password';
  for (const [error, key] of Object.entries({nameFormat:'name',textFormat:'text',phone:'phone',identifier:'selection',integerRange:'integerRange',priceRange:'price',moneyPrecision:'moneyPrecision',allowedValue:'selection',pattern:'format',min:'min',max:'max'})) {
    if (errors[error]) return 'validation.' + key;
  }
  return 'validation.format';
}

export function applyServerError(form: FormGroup, err: unknown): void {
  const failure = err as { message?: string; response?: { fieldErrors?: Record<string, string[]> } };
  const fields = failure?.response?.fieldErrors ?? {};
  for (const [path, messages] of Object.entries(fields)) {
    const parts = path.replace(/^\$\./, '').split('.');
    const direct = parts[0].charAt(0).toLowerCase() + parts[0].slice(1);
    const parent = parts.length > 1 ? parts[parts.length - 2].toLowerCase() : '';
    const leaf = parts[parts.length - 1];
    const localized = parent === 'name' ? 'name' + leaf : parent === 'description' ? 'desc' + leaf : '';
    const key = form.contains(direct) ? direct : form.contains(localized) ? localized : leaf.charAt(0).toLowerCase() + leaf.slice(1);
    const control = form.get(key);
    if (control) { control.setErrors({ ...control.errors, server: messages.join(' ') }); control.markAsTouched(); }
  }
  if (failure?.message) form.setErrors({ ...form.errors, server: failure.message });
}
