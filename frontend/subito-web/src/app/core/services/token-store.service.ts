import { Injectable, computed, signal } from '@angular/core';
import { AuthResponseDto, Portal, UserDto } from '../models/api.models';

function readPermissionClaims(json: string): string[] {
  const values: string[] = [];
  const re = /"([^"]*permission[^"]*)"\s*:\s*(\[[\s\S]*?\]|"[^"]*")/gi;
  let match: RegExpExecArray | null;
  while ((match = re.exec(json))) {
    const raw = match[2];
    if (raw.startsWith('[')) {
      try {
        const parsed = JSON.parse(raw) as unknown;
        if (Array.isArray(parsed)) values.push(...parsed.map(String));
      } catch {
        /* skip malformed claim */
      }
    } else {
      values.push(JSON.parse(raw) as string);
    }
  }
  return [...new Set(values)];
}

interface StoredSession {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAtUtc: string;
  user: UserDto;
}

@Injectable({ providedIn: 'root' })
export class TokenStoreService {
  private readonly keys: Record<Portal, string> = {
    client: 'subito.client',
    provider: 'subito.provider',
    admin: 'subito.admin',
  };

  private readonly sessions = signal<Record<Portal, StoredSession | null>>({
    client: this.read('client'),
    provider: this.read('provider'),
    admin: this.read('admin'),
  });

  clientUser = computed(() => this.sessions().client?.user ?? null);
  providerUser = computed(() => this.sessions().provider?.user ?? null);
  adminUser = computed(() => this.sessions().admin?.user ?? null);

  getAccessToken(portal: Portal): string | null {
    return this.sessions()[portal]?.accessToken ?? null;
  }

  getRefreshToken(portal: Portal): string | null {
    return this.sessions()[portal]?.refreshToken ?? null;
  }

  getUser(portal: Portal): UserDto | null {
    return this.sessions()[portal]?.user ?? null;
  }

  isAuthenticated(portal: Portal): boolean {
    return !!this.getAccessToken(portal);
  }

  isAccessTokenExpired(portal: Portal): boolean {
    const expiresAt = this.sessions()[portal]?.accessTokenExpiresAtUtc;
    if (!expiresAt) return false;
    const expiresMs = Date.parse(expiresAt);
    if (Number.isNaN(expiresMs)) return false;
    return expiresMs <= Date.now() + 15_000;
  }

  save(portal: Portal, auth: AuthResponseDto): void {
    const session: StoredSession = {
      accessToken: auth.accessToken,
      refreshToken: auth.refreshToken,
      accessTokenExpiresAtUtc: auth.accessTokenExpiresAtUtc,
      user: auth.user,
    };
    localStorage.setItem(this.keys[portal], JSON.stringify(session));
    this.sessions.update((s) => ({ ...s, [portal]: session }));
  }

  clear(portal: Portal): void {
    localStorage.removeItem(this.keys[portal]);
    this.sessions.update((s) => ({ ...s, [portal]: null }));
  }

  getPermissions(portal: Portal): string[] {
    const token = this.getAccessToken(portal);
    if (!token) return [];
    try {
      const segment = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
      const padded = segment + '='.repeat((4 - (segment.length % 4)) % 4);
      const json = atob(padded);
      const fromRaw = readPermissionClaims(json);
      if (fromRaw.length) return fromRaw;
      const payload = JSON.parse(json) as Record<string, unknown>;
      const values: string[] = [];
      for (const [key, claim] of Object.entries(payload)) {
        if (!/permission/i.test(key)) continue;
        if (Array.isArray(claim)) values.push(...claim.map(String));
        else if (typeof claim === 'string') values.push(claim);
      }
      return values;
    } catch {
      return [];
    }
  }

  hasPermission(portal: Portal, permission: string): boolean {
    return this.getPermissions(portal).includes(permission);
  }

  hasAnyPermission(portal: Portal, permissions: string[]): boolean {
    const set = new Set(this.getPermissions(portal));
    return permissions.some((p) => set.has(p));
  }

  private read(portal: Portal): StoredSession | null {
    try {
      const raw = localStorage.getItem(this.keys[portal]);
      return raw ? (JSON.parse(raw) as StoredSession) : null;
    } catch {
      return null;
    }
  }
}
