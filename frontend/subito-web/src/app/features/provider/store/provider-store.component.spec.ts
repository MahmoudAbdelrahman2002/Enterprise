import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { I18nService } from '../../../core/services/i18n.service';
import { StoreService } from '../../../core/services/store.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { ProviderStoreComponent } from './provider-store.component';

describe('ProviderStoreComponent permissions', () => {
  const store = {
    id: 'store', companyName: 'Original store', phoneNumber: '+201000000030',
    imageUrl: null, serviceId: null, serviceName: 'Restaurant',
  };
  let api: jasmine.SpyObj<StoreService>;
  let tokens: jasmine.SpyObj<TokenStoreService>;

  beforeEach(async () => {
    api = jasmine.createSpyObj<StoreService>('StoreService', ['get', 'update', 'uploadImage']);
    api.get.and.returnValue(of(store));
    api.update.and.returnValue(of(store));
    api.uploadImage.and.returnValue(of({}));
    tokens = jasmine.createSpyObj<TokenStoreService>('TokenStoreService', ['hasPermission']);
    await TestBed.configureTestingModule({
      imports: [ProviderStoreComponent],
      providers: [
        { provide: StoreService, useValue: api },
        { provide: TokenStoreService, useValue: tokens },
        { provide: I18nService, useValue: { t: (key: string) => key } },
      ],
    }).compileComponents();
  });

  it('shows disabled identity fields and hides writes for store readers', () => {
    tokens.hasPermission.and.returnValue(false);
    const fixture = TestBed.createComponent(ProviderStoreComponent);
    fixture.detectChanges();
    const page = fixture.nativeElement as HTMLElement;

    expect((page.querySelector('#provider-store-companyName') as HTMLInputElement).disabled).toBeTrue();
    expect((page.querySelector('#provider-store-phoneNumber') as HTMLInputElement).disabled).toBeTrue();
    expect(page.querySelector('button[type="submit"]')).toBeNull();
    expect(page.querySelector('input[type="file"]')).toBeNull();
    fixture.componentInstance.save();
    fixture.componentInstance.onFile(new File(['probe'], 'probe.png', { type: 'image/png' }));
    expect(api.update).not.toHaveBeenCalled();
    expect(api.uploadImage).not.toHaveBeenCalled();
    expect(tokens.hasPermission).toHaveBeenCalledWith('provider', 'ProviderStore.Update');
  });

  it('lets store editors save identity and upload images', () => {
    tokens.hasPermission.and.returnValue(true);
    const fixture = TestBed.createComponent(ProviderStoreComponent);
    fixture.detectChanges();
    const page = fixture.nativeElement as HTMLElement;

    expect((page.querySelector('#provider-store-companyName') as HTMLInputElement).disabled).toBeFalse();
    expect(page.querySelector('button[type="submit"]')).not.toBeNull();
    expect(page.querySelector('input[type="file"]')).not.toBeNull();
    fixture.componentInstance.form.setValue({ companyName: 'Updated store', phoneNumber: '+201000000099' });
    fixture.componentInstance.save();
    expect(api.update).toHaveBeenCalledWith('Updated store', '+201000000099');
    const file = new File(['probe'], 'probe.png', { type: 'image/png' });
    fixture.componentInstance.onFile(file);
    expect(api.uploadImage).toHaveBeenCalledWith(file);
  });
});
