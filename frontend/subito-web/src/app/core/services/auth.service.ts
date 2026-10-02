import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AuthResponseDto, OtpSentDto, Portal, ProfileDto } from '../models/api.models';
import { ApiService } from './api.service';
import { TokenStoreService } from './token-store.service';
import { ToastService } from './toast.service';
import { I18nService } from './i18n.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  constructor(
    private readonly api: ApiService,
    private readonly tokens: TokenStoreService,
    private readonly toast: ToastService,
    private readonly router: Router,
    private readonly i18n: I18nService
  ) {}

  // --- Client OTP ---
  clientRegister(body: { firstName: string; lastName: string; email: string }): Observable<OtpSentDto> {
    return this.api.post<OtpSentDto>('/client/auth/register', body);
  }

  clientVerifyRegistration(body: { email: string; otp: string }): Observable<AuthResponseDto> {
    return this.api.post<AuthResponseDto>('/client/auth/verify-registration', body).pipe(
      tap((auth) => this.tokens.save('client', auth))
    );
  }

  clientLogin(body: { email: string }): Observable<OtpSentDto> {
    return this.api.post<OtpSentDto>('/client/auth/login', body);
  }

  clientVerifyLogin(body: { email: string; otp: string }): Observable<AuthResponseDto> {
    return this.api.post<AuthResponseDto>('/client/auth/verify-login', body).pipe(
      tap((auth) => this.tokens.save('client', auth))
    );
  }

  clientExternal(body: { provider: string; idToken: string }): Observable<AuthResponseDto> {
    return this.api.post<AuthResponseDto>('/client/auth/external', body).pipe(
      tap((auth) => this.tokens.save('client', auth))
    );
  }

  // --- Provider / Admin password ---
  passwordLogin(portal: 'provider' | 'admin', body: { email: string; password: string }): Observable<AuthResponseDto> {
    return this.api.post<AuthResponseDto>(`/${portal}/auth/login`, body).pipe(
      tap((auth) => this.tokens.save(portal, auth))
    );
  }

  forgotPassword(portal: 'provider' | 'admin', body: { email: string }): Observable<OtpSentDto> {
    return this.api.post<OtpSentDto>(`/${portal}/auth/forgot-password`, body);
  }

  resetPassword(
    portal: 'provider' | 'admin',
    body: { email: string; otp: string; newPassword: string }
  ): Observable<unknown> {
    return this.api.post(`/${portal}/auth/reset-password`, body);
  }

  changePassword(
    portal: Portal,
    body: { currentPassword: string; newPassword: string }
  ): Observable<unknown> {
    return this.api.post(`/${portal}/auth/change-password`, body);
  }

  refresh(portal: Portal): Observable<AuthResponseDto> {
    const refreshToken = this.tokens.getRefreshToken(portal);
    return this.api.post<AuthResponseDto>(`/${portal}/auth/refresh-token`, { refreshToken }).pipe(
      tap((auth) => this.tokens.save(portal, auth))
    );
  }

  logout(portal: Portal, navigateTo?: string): void {
    const refreshToken = this.tokens.getRefreshToken(portal);
    if (refreshToken) {
      this.api.post(`/${portal}/auth/revoke-token`, { refreshToken }).subscribe({
        error: () => undefined,
      });
    }
    this.tokens.clear(portal);
    this.toast.info(this.i18n.t('auth.signedOut'));
    void this.router.navigateByUrl(navigateTo ?? this.loginPath(portal));
  }

  getProfile(portal: Portal): Observable<ProfileDto> {
    return this.api.get<ProfileDto>(`/${portal}/profile`);
  }

  updateProfile(portal: Portal, body: { firstName: string; lastName: string }): Observable<ProfileDto> {
    return this.api.put<ProfileDto>(`/${portal}/profile`, body);
  }

  requestEmailChange(portal: Portal, body: { newEmail: string }): Observable<OtpSentDto> {
    return this.api.post<OtpSentDto>(`/${portal}/profile/change-email/request`, body);
  }

  confirmEmailChange(portal: Portal, body: { newEmail: string; otp: string }): Observable<ProfileDto> {
    return this.api.post<ProfileDto>(`/${portal}/profile/change-email/confirm`, body);
  }

  private loginPath(portal: Portal): string {
    if (portal === 'client') return '/auth/login';
    if (portal === 'provider') return '/provider/login';
    return '/admin/login';
  }
}
