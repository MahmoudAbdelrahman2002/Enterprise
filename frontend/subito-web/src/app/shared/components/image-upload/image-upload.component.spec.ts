import { TestBed } from '@angular/core/testing';
import { ImageUploadComponent } from './image-upload.component';
import { imageError } from '../../forms/image-validation';
import { I18nService } from '../../../core/services/i18n.service';
import { ToastService } from '../../../core/services/toast.service';

function png(): File {
  const binary = atob('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jr1kAAAAASUVORK5CYII=');
  return new File([Uint8Array.from(binary, c => c.charCodeAt(0))], 'pixel.png', { type: 'image/png' });
}
describe('image uploads', () => {
  it('accepts PNG content and rejects disguised files and unsupported types', async () => {
    expect(await imageError(png())).toBeNull();
    expect(await imageError(new File(['text'], 'fake.png', { type: 'image/png' }))).toBe('validation.imageContent');
    expect(await imageError(new File(['GIF89a'], 'image.gif', { type: 'image/gif' }))).toBe('validation.imageType');
  });
  it('emits the exact file that was validated even if the input changes during validation', async () => {
    TestBed.configureTestingModule({ providers: [
      { provide: I18nService, useValue: { t: (key: string) => key } },
      { provide: ToastService, useValue: { error: jasmine.createSpy('error') } },
    ] });
    const component = TestBed.runInInjectionContext(() => new ImageUploadComponent());
    const selected = jasmine.createSpy('selected');
    component.selected.subscribe(selected);
    const valid = png();
    const input = { files: [valid], value: 'pixel.png' };
    const pending = component.choose({ target: input } as unknown as Event);
    input.files = [new File(['text'], 'fake.png', { type: 'image/png' })];
    await pending;
    expect(selected).toHaveBeenCalledOnceWith(valid);
    expect(input.value).toBe('');
  });
});
