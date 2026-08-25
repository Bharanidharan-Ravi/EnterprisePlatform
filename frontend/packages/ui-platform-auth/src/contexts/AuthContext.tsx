import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useRef,
  type ReactNode,
} from 'react';
import { getApiClient, getAppConfig, isApiError, HttpStatus } from '@nucleus/uiplatform-foundation';
import type { AxiosError, AxiosRequestConfig } from 'axios';
import { authStore } from '../stores/authStore';
import { AuthService } from '../services/AuthService';
import { TokenService } from '../services/TokenService';
import { SessionService } from '../services/SessionService';
import type { AuthenticationResponse, LoginCredentials } from '../types';

interface AuthContextValue {
  login: (credentials: LoginCredentials) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

/** Interpolates an AuthenticationResponse into the store + TokenService storage, or clears both on failure. */
function applyAuthResponse(response: AuthenticationResponse, rememberMe: boolean): boolean {
  if (!response.ok || !response.accessToken) {
    authStore.getState().setError(response.errorMessage ?? 'Authentication failed.');
    return false;
  }

  const claims = TokenService.decode(response.accessToken);
  const session = {
    accessToken: response.accessToken,
    refreshToken: response.refreshToken ?? null,
    expiresAt: response.expiresAt ?? null,
    sessionId: response.sessionId ?? null,
    user: response.user ?? null,
    claims,
  };

  TokenService.save(session, rememberMe);
  authStore.getState().setSession(session);
  return true;
}

/**
 * Composition root for auth: bootstraps a persisted session on mount, schedules silent
 * refresh/expiry, and installs the 401-retry-after-refresh interceptor on Foundation's shared
 * axios client. Mount this once, inside AppProvider, near the root.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  // Dedupes concurrent refresh attempts from SessionService's timer and the 401 interceptor.
  const refreshInFlight = useRef<Promise<boolean> | null>(null);
  const rememberMeRef = useRef(true);

  const performRefresh = useMemo(
    () => async (): Promise<boolean> => {
      if (refreshInFlight.current) return refreshInFlight.current;

      const attempt = (async () => {
        const { refreshToken, user } = authStore.getState();
        if (!refreshToken || !user?.userId) return false;
        try {
          const response = await AuthService.refresh(refreshToken, user.userId);
          return applyAuthResponse(response, rememberMeRef.current);
        } catch {
          return false;
        }
      })();

      refreshInFlight.current = attempt;
      const result = await attempt;
      refreshInFlight.current = null;
      return result;
    },
    [],
  );

  const scheduleFromClaims = useMemo(
    () => () => {
      const { claims } = authStore.getState();
      if (!claims) return;
      SessionService.schedule(
        claims,
        () => void performRefresh(),
        () => authStore.getState().expireSession(),
      );
    },
    [performRefresh],
  );

  // Bootstrap: restore persisted session, or silently refresh, on first mount.
  useEffect(() => {
    (async () => {
      const persisted = TokenService.load();
      if (!persisted?.accessToken) {
        authStore.getState().setInitializing(false);
        return;
      }

      const claims = TokenService.decode(persisted.accessToken);
      if (claims && !TokenService.isExpired(claims)) {
        authStore.getState().setSession({ ...persisted, claims });
        scheduleFromClaims();
        return;
      }

      if (persisted.refreshToken) {
        authStore.getState().setSession(persisted); // provisional; refreshToken enables performRefresh()
        const ok = await performRefresh();
        if (ok) scheduleFromClaims();
        else authStore.getState().expireSession();
        return;
      }

      authStore.getState().clearSession();
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Re-schedule whenever a new session is applied (login, refresh).
  useEffect(() => authStore.subscribe((state, prevState) => {
    if (state.accessToken && state.accessToken !== prevState.accessToken) scheduleFromClaims();
  }), [scheduleFromClaims]);

  // 401-retry-after-refresh interceptor. Foundation's client never implements this itself
  // (see UIPlatform.Foundation/api/interceptors.ts comments) — it's this package's job.
  useEffect(() => {
    const client = getApiClient(getAppConfig());
    const interceptorId = client.interceptors.response.use(
      (response) => response,
      async (error: AxiosError) => {
        const status = isApiError(error) ? error.status : error.response?.status;
        const original = error.config as (AxiosRequestConfig & { _retried?: boolean }) | undefined;

        if (status !== HttpStatus.UNAUTHORIZED || !original || original._retried) {
          return Promise.reject(error);
        }

        original._retried = true;
        const refreshed = await performRefresh();
        if (!refreshed) {
          authStore.getState().expireSession();
          return Promise.reject(error);
        }

        return client.request(original);
      },
    );

    return () => client.interceptors.response.eject(interceptorId);
  }, [performRefresh]);

  const value = useMemo<AuthContextValue>(
    () => ({
      login: async (credentials) => {
        rememberMeRef.current = credentials.rememberMe ?? true;
        authStore.getState().setError(null);
        const response = await AuthService.login(credentials);
        const ok = applyAuthResponse(response, rememberMeRef.current);
        if (!ok) throw new Error(response.errorMessage ?? 'Authentication failed.');
      },
      logout: async () => {
        const { refreshToken, sessionId } = authStore.getState();
        SessionService.clear();
        try {
          await AuthService.logout({ refreshToken: refreshToken ?? undefined, sessionId: sessionId ?? undefined });
        } finally {
          authStore.getState().clearSession();
        }
      },
    }),
    [],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

/** Internal accessor — prefer useAuth() from hooks/useAuth for the public surface. */
export function useAuthContext(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuthContext must be used within an AuthProvider');
  return ctx;
}
