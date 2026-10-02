import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, provideRouter } from '@angular/router';
import { authGuard, permissionGuard } from './auth.guards';
import { TokenStoreService } from '../services/token-store.service';

describe('route guards', () => {
  function setup(tokens: Partial<TokenStoreService>): Router {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: TokenStoreService, useValue: tokens }],
    });
    return TestBed.inject(Router);
  }

  it('sends an anonymous admin visitor to the admin login', () => {
    const router = setup({ isAuthenticated: () => false, hasAnyPermission: () => false });
    const result = TestBed.runInInjectionContext(() => authGuard('admin')({} as never, {} as never));
    expect(result).toBeInstanceOf(UrlTree);
    expect(router.serializeUrl(result as UrlTree)).toBe('/admin/login');
  });

  it('allows an authenticated user through', () => {
    setup({ isAuthenticated: () => true, hasAnyPermission: () => true });
    const result = TestBed.runInInjectionContext(() => authGuard('provider')({} as never, {} as never));
    expect(result).toBeTrue();
  });

  it('sends a user without the required permission back home', () => {
    const router = setup({ isAuthenticated: () => true, hasAnyPermission: () => false });
    const result = TestBed.runInInjectionContext(() =>
      permissionGuard('admin', ['Roles.Create'])({} as never, {} as never)
    );
    expect(router.serializeUrl(result as UrlTree)).toBe('/admin');
  });
});
