import type { AuthSession, DecodedAccessToken } from '../types';

const STORAGE_KEY = 'nucleus.auth.session';
const KNOWN_CLAIM_KEYS = new Set([
  'sub',
  'jti',
  'iat',
  'exp',
  'role',
  'permission',
  'tenant_id',
  'company_id',
  'branch_id',
  'department_id',
  'sid',
  // ClaimTypes.NameIdentifier / ClaimTypes.Name / ClaimTypes.Email full URIs used by JwtService
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier',
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name',
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress',
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
]);

/**
 * Decodes and persists JWT/session state. No network calls — pure token + storage concerns.
 * Storage backend is chosen per-session (rememberMe -> localStorage, otherwise sessionStorage)
 * so nothing here assumes a fixed persistence strategy.
 */
class TokenServiceImpl {
  private backend: Storage | null = null;

  /** Decodes a JWT's payload without verifying the signature (verification is server-side only). */
  decode(accessToken: string): DecodedAccessToken | null {
    try {
      const payload = accessToken.split('.')[1];
      if (!payload) return null;
      const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
      const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), '=');
      const json = JSON.parse(atob(padded)) as Record<string, unknown>;

      const role = this.toStringArray(json.role ?? json['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']);
      const permission = this.toStringArray(json.permission);

      const extra: Record<string, string> = {};
      for (const [key, value] of Object.entries(json)) {
        if (!KNOWN_CLAIM_KEYS.has(key) && typeof value === 'string') {
          extra[key] = value;
        }
      }

      return {
        sub: String(json.sub ?? ''),
        role,
        permission,
        tenantId: this.asString(json.tenant_id),
        companyId: this.asString(json.company_id),
        branchId: this.asString(json.branch_id),
        departmentId: this.asString(json.department_id),
        sessionId: this.asString(json.sid),
        exp: Number(json.exp ?? 0),
        extra,
      };
    } catch {
      return null;
    }
  }

  isExpired(claims: DecodedAccessToken | null, leadSeconds = 0): boolean {
    if (!claims?.exp) return true;
    return Date.now() / 1000 >= claims.exp - leadSeconds;
  }

  /** Persists the session to the chosen storage backend. Pass rememberMe to select/switch backend. */
  save(session: AuthSession, rememberMe: boolean): void {
    this.backend = rememberMe ? window.localStorage : window.sessionStorage;
    const other = rememberMe ? window.sessionStorage : window.localStorage;
    other.removeItem(STORAGE_KEY);
    this.backend.setItem(STORAGE_KEY, JSON.stringify(session));
  }

  /** Loads a previously persisted session, checking both storage backends. */
  load(): AuthSession | null {
    const raw = window.localStorage.getItem(STORAGE_KEY) ?? window.sessionStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as AuthSession;
    } catch {
      return null;
    }
  }

  clear(): void {
    window.localStorage.removeItem(STORAGE_KEY);
    window.sessionStorage.removeItem(STORAGE_KEY);
  }

  private toStringArray(value: unknown): string[] {
    if (Array.isArray(value)) return value.map(String);
    if (typeof value === 'string') return [value];
    return [];
  }

  private asString(value: unknown): string | undefined {
    return typeof value === 'string' ? value : undefined;
  }
}

export const TokenService = new TokenServiceImpl();
