import type { DecodedAccessToken } from '../types';
import { getAuthConfig } from '../config/authConfig';

type TimerHandle = ReturnType<typeof setTimeout>;

/**
 * Schedules the two client-side timers every session needs: a silent-refresh attempt shortly
 * before expiry, and a hard expiry callback if refresh never happens. Pure timer orchestration —
 * callers supply what "refresh" and "expire" mean (AuthContext wires these to AuthService/store).
 */
class SessionServiceImpl {
  private refreshTimer: TimerHandle | null = null;
  private expiryTimer: TimerHandle | null = null;

  schedule(claims: DecodedAccessToken, onSilentRefreshDue: () => void, onExpired: () => void): void {
    this.clear();

    const { silentRefreshLeadMinutes } = getAuthConfig();
    const expiresAtMs = claims.exp * 1000;
    const leadMs = silentRefreshLeadMinutes * 60 * 1000;
    const now = Date.now();

    const refreshInMs = Math.max(expiresAtMs - leadMs - now, 0);
    const expireInMs = Math.max(expiresAtMs - now, 0);

    this.refreshTimer = setTimeout(onSilentRefreshDue, refreshInMs);
    this.expiryTimer = setTimeout(onExpired, expireInMs);
  }

  clear(): void {
    if (this.refreshTimer) clearTimeout(this.refreshTimer);
    if (this.expiryTimer) clearTimeout(this.expiryTimer);
    this.refreshTimer = null;
    this.expiryTimer = null;
  }
}

export const SessionService = new SessionServiceImpl();
