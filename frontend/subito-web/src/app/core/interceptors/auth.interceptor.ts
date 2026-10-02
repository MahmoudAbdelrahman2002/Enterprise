import { HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, shareReplay, switchMap, throwError } from 'rxjs';
import { AuthResponseDto, Portal } from '../models/api.models';
import { AuthService } from '../services/auth.service';
import { TokenStoreService } from '../services/token-store.service';

const refreshInFlight = new Map<Portal, Observable<AuthResponseDto>>();

function refreshOnce(auth: AuthService, portal: Portal): Observable<AuthResponseDto> {
  const existing = refreshInFlight.get(portal);
  if (existing) return existing;
  const shared = auth.refresh(portal).pipe(
    finalize(() => refreshInFlight.delete(portal)),
    shareReplay({ bufferSize: 1, refCount: false })
  );
  refreshInFlight.set(portal, shared);
  return shared;
}

function withToken(req: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  if (!token) return req;
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

function loginPath(portal: Portal): string {
  if (portal === 'client') return '/auth/login';
  if (portal === 'provider') return '/provider/login';
  return '/admin/login';
}

function resolvePortal(url: string): Portal | null {
  if (url.includes('/client/')) return 'client';
  if (url.includes('/provider/')) return 'provider';
  if (url.includes('/admin/')) return 'admin';
  return null;
}

function isAuthAnonymous(url: string): boolean {
  return (
    /\/auth\/(login|register|verify-|refresh-token|forgot-password|reset-password|external|revoke-token)/.test(
      url
    )
  );
}

/** Storefront catalog is public. A stale client token must not block these reads. */
function isPublicCatalog(url: string): boolean {
  return (
    /\/client\/services(\/|$|\?)/.test(url) ||
    /\/client\/providers(\/|$|\?)/.test(url) ||
    /\/client\/products(\/|$|\?)/.test(url) ||
    /\/client\/categories\/[^/]+\/products(\/|$|\?)/.test(url) ||
    /\/client\/[0-9a-f-]{36}\/(categories|products)(\/|$|\?)/i.test(url)
  );
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokens = inject(TokenStoreService);
  const auth = inject(AuthService);
  const router = inject(Router);
  const portal = resolvePortal(req.url);
  const anonymous = !portal || isAuthAnonymous(req.url) || isPublicCatalog(req.url);

  const failSession = (refreshErr: unknown) => {
    if (portal) tokens.clear(portal);
    if (portal) void router.navigateByUrl(loginPath(portal));
    return throwError(() => refreshErr);
  };

  const send = (request: HttpRequest<unknown>) =>
    next(request).pipe(
      catchError((err) => {
        if (err?.status === 401 && portal && !anonymous) {
          if (tokens.getRefreshToken(portal)) {
            return refreshOnce(auth, portal).pipe(
              switchMap(() => next(withToken(req, tokens.getAccessToken(portal)))),
              catchError((refreshErr) => failSession(refreshErr))
            );
          }
          return failSession(err);
        }
        return throwError(() => err);
      })
    );

  if (!anonymous && portal && tokens.isAccessTokenExpired(portal) && tokens.getRefreshToken(portal)) {
    return refreshOnce(auth, portal).pipe(
      switchMap(() => send(withToken(req, tokens.getAccessToken(portal)))),
      catchError((refreshErr) => failSession(refreshErr))
    );
  }

  const token = anonymous || !portal ? null : tokens.getAccessToken(portal);
  return send(withToken(req, token));
};
