import { AbstractControl, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { VALIDATION_POLICY as P } from './validation-policy';

export const readableText = (multiline = false, allowWhitespace = false): ValidatorFn => control => {
  const value = String(control.value ?? '');
  if (!value) return null;
  return (!allowWhitespace && !value.trim()) || (multiline ? /[\x00-\x08\x0b\x0c\x0e-\x1f\x7f-\x9f]/ : /[\x00-\x1f\x7f-\x9f]/).test(value) ? { textFormat: true } : null;
};
export const personName: ValidatorFn = control => {
  const value = String(control.value ?? '');
  return !value || (/^[\p{L}\p{M} .\u2019'\-]+$/u.test(value) && /\p{L}/u.test(value)) ? null : { nameFormat: true };
};
export const realisticEmail: ValidatorFn = control => {
  const value = String(control.value ?? '');
  if (!value) return null;
  const parts = value.split('@');
  if (value !== value.trim() || value.length > P.EmailMax || parts.length !== 2) return { email: true };
  const [local, domain] = parts;
  if (local.length > 64 || !/^[A-Za-z0-9.!#$%&'*+/=?^_`{|}~\-]+$/.test(local) || local.startsWith('.') || local.endsWith('.') || local.includes('..')) return { email: true };
  try {
    if (/[\s%/:?#\[\]\\]/.test(domain)) return { email: true };
    const ascii = new URL('http://' + domain).hostname;
    const labels = ascii.split('.');
    return ascii.length <= 253 && labels.length >= 2 && labels.every(label => label.length >= 1 && label.length <= 63 && /^[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?$/.test(label)) ? null : { email: true };
  } catch { return { email: true }; }
};
export const phoneNumber: ValidatorFn = control => {
  const value = String(control.value ?? '');
  if (!value) return null;
  if (!/^\+?[0-9 ()-]+$/.test(value)) return { phone: true };
  const count = (value.match(/[0-9]/g) ?? []).length;
  let depth = 0;
  for (const c of value) {
    if (c === '(' && ++depth > 1) return { phone: true };
    if (c === ')' && --depth < 0) return { phone: true };
  }
  return depth === 0 && count >= P.PhoneDigitsMin && count <= P.PhoneDigitsMax ? null : { phone: true };
};
export const nonemptyId: ValidatorFn = control => {
  const value = String(control.value ?? '');
  if (!value) return null;
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value) && !/^0{8}-0{4}-0{4}-0{4}-0{12}$/.test(value) ? null : { identifier: true };
};
export const integerRange = (min: number, max: number): ValidatorFn => control => {
  if (control.value === null || control.value === '') return null;
  const n = Number(control.value);
  return Number.isSafeInteger(n) && n >= min && n <= max ? null : { integerRange: { min, max } };
};
export const productPrice: ValidatorFn = control => {
  if (control.value === null || control.value === '') return null;
  const value = Number(control.value);
  if (!Number.isFinite(value) || value < .01 || value > P.PriceMax) return { priceRange: true };
  return Math.abs(value * 100 - Math.round(value * 100)) < 1e-8 ? null : { moneyPrecision: true };
};
export const strongPassword: ValidatorFn = control => {
  const value = String(control.value ?? '');
  if (!value) return null;
  if (value.length > P.PasswordMax) return { maxlength: { requiredLength: P.PasswordMax, actualLength: value.length } };
  if (/[\x00-\x1f\x7f-\x9f]/.test(value)) return { textFormat: true };
  if (value.length < P.PasswordMin || !/[A-Z]/.test(value) || !/[a-z]/.test(value) || !/[0-9]/.test(value) || !/[^A-Za-z0-9]/.test(value)) return { passwordStrength: true };
  return null;
};
export const differentPasswords: ValidatorFn = control => {
  const current = control.get('currentPassword')?.value;
  const next = control.get('newPassword')?.value;
  return current && next && current === next ? { passwordDifferent: true } : null;
};

export const fieldRules = {
  email: [Validators.required, realisticEmail, Validators.maxLength(P.EmailMax)],
  name: [Validators.required, readableText(), personName, Validators.maxLength(P.NameMax)],
  staffName: [Validators.required, readableText(), personName, Validators.maxLength(P.NameMax)],
  company: [Validators.required, readableText(), Validators.maxLength(P.CompanyMax)],
  title: [Validators.required, readableText(), Validators.maxLength(P.TitleMax)],
  roleName: [Validators.required, readableText(), Validators.maxLength(P.RoleNameMax)],
  optionalTitle: [readableText(), Validators.maxLength(P.TitleMax)],
  optionalRoleName: [readableText(), Validators.maxLength(P.RoleNameMax)],
  description: [readableText(true), Validators.maxLength(P.DescriptionMax)],
  phone: [phoneNumber, Validators.maxLength(P.PhoneMax)],
  password: [Validators.required, strongPassword],
  existingPassword: [Validators.required, readableText(), Validators.maxLength(P.PasswordMax)],
  otp: [Validators.required, Validators.pattern(new RegExp(`^[0-9]{${P.OtpLength}}$`))],
  id: [Validators.required, nonemptyId],
  optionalId: [nonemptyId],
  code: [Validators.required, Validators.maxLength(P.CodeMax), Validators.pattern(/^[A-Za-z0-9][A-Za-z0-9_-]*$/)],
  sku: [Validators.required, Validators.maxLength(P.SkuMax), Validators.pattern(/^[A-Za-z0-9][A-Za-z0-9_-]*$/)],
  price: [Validators.required, productPrice],
  displayOrder: [Validators.required, integerRange(0, P.DisplayOrderMax)],
  productStatus: [Validators.required, (control: AbstractControl): ValidationErrors | null => [0, 1, 2].includes(Number(control.value)) ? null : { allowedValue: true }],
  search: [readableText(false, true), Validators.maxLength(P.SearchMax)],
};
