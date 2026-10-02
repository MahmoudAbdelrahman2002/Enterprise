import { TokenStoreService } from './token-store.service';

function token(payload: string): string {
  const segment = btoa(payload).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `header.${segment}.sig`;
}

describe('TokenStoreService permissions', () => {
  afterEach(() => localStorage.clear());

  it('reads a permission array from an unpadded JWT payload', () => {
    localStorage.setItem('subito.admin', JSON.stringify({
      accessToken: token('{"permission":["Roles.Read","Roles.Create"]}'),
      refreshToken: 'refresh',
      accessTokenExpiresAtUtc: '2099-01-01T00:00:00Z',
      user: { id: '1', email: 'a@b.c', firstName: 'A', lastName: 'B', userType: 2, roles: [] },
    }));

    const store = new TokenStoreService();
    expect(store.getPermissions('admin')).toEqual(['Roles.Read', 'Roles.Create']);
    expect(store.hasPermission('admin', 'Roles.Create')).toBeTrue();
    expect(store.hasAnyPermission('admin', ['Missing', 'Roles.Read'])).toBeTrue();
  });

  it('collects duplicate permission claim keys', () => {
    localStorage.setItem('subito.provider', JSON.stringify({
      accessToken: token('{"permission":"ProviderStaff.Create","permission":"ProviderProduct.Read"}'),
      refreshToken: 'refresh',
      accessTokenExpiresAtUtc: '2099-01-01T00:00:00Z',
      user: { id: '1', email: 'a@b.c', firstName: 'A', lastName: 'B', userType: 3, roles: [] },
    }));

    const store = new TokenStoreService();
    expect(store.getPermissions('provider')).toEqual(['ProviderStaff.Create', 'ProviderProduct.Read']);
  });
});
