import { FormControl, FormGroup } from '@angular/forms';
import { fieldRules, differentPasswords, personName, phoneNumber, productPrice, realisticEmail } from './field-validators';
import { firstErrorKey, applyServerError } from './form-errors';
import * as fixtures from '../../../../../../tests/validation-cases.json';

describe('Shared validation policy', () => {
  it('agrees with backend fixtures for international names, email, phones, money and OTPs', () => {
    const cases = fixtures as unknown as Record<string, [string | number, boolean][]>;
    const validators = {names:personName,emails:realisticEmail,phones:phoneNumber,prices:productPrice};
    for (const [group, validator] of Object.entries(validators)) {
      for (const [value, expected] of cases[group]) expect(validator(new FormControl(value)) === null).withContext(`${group}: ${value}`).toBe(expected);
    }
    for (const [value, expected] of cases['otps']) expect(new FormControl(value, fieldRules.otp).valid).withContext(`otp: ${value}`).toBe(expected);
  });
  it('does not describe a name length error as a password error', () => {
    const control = new FormControl('x'.repeat(101), fieldRules.name);
    control.markAsTouched();
    expect(firstErrorKey(control)).toBe('validation.maxLength');
  });
  it('rejects reusing the current password', () => {
    const form = new FormGroup({currentPassword:new FormControl('Strong123!'),newPassword:new FormControl('Strong123!')}, {validators:differentPasswords});
    expect(form.hasError('passwordDifferent')).toBeTrue();
  });
  it('attaches nested backend errors to the correct field', () => {
    const form = new FormGroup({nameEn:new FormControl(''),phoneNumber:new FormControl('')});
    applyServerError(form,{message:'Check your details',response:{fieldErrors:{'Product.Name.En':['Name is too long'],PhoneNumber:['Enter a valid phone number']}}});
    expect(form.controls.nameEn.errors?.['server']).toBe('Name is too long');
    expect(form.controls.phoneNumber.errors?.['server']).toBe('Enter a valid phone number');
  });
});
