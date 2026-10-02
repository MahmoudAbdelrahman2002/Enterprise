import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Portal } from '../models/api.models';
import { TokenStoreService } from '../services/token-store.service';

function loginUrl(portal: Portal): string {
  if (portal === 'client') return '/auth/login';
  if (portal === 'provider') return '/provider/login';
  return '/admin/login';
}

export function authGuard(portal: Portal): CanActivateFn {
  return () => {
    const tokens = inject(TokenStoreService);
    const router = inject(Router);
    if (tokens.isAuthenticated(portal)) return true;
    return router.createUrlTree([loginUrl(portal)]);
  };
}

export function permissionGuard(portal: Portal, permissions: string[]): CanActivateFn {
  return () => {
    const tokens = inject(TokenStoreService);
    const router = inject(Router);
    if (!tokens.isAuthenticated(portal)) {
      return router.createUrlTree([loginUrl(portal)]);
    }
    if (!permissions.length || tokens.hasAnyPermission(portal, permissions)) {
      return true;
    }
    const home =
      portal === 'admin' ? '/admin' : portal === 'provider' ? '/provider' : '/';
    return router.createUrlTree([home]);
  };
}

export function guestGuard(portal: Portal, redirectTo: string): CanActivateFn {
  return () => {
    const tokens = inject(TokenStoreService);
    const router = inject(Router);
    if (tokens.isAuthenticated(portal)) {
      return router.createUrlTree([redirectTo]);
    }
    return true;
  };
}
