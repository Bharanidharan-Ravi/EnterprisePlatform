import { createStore } from '@nucleus/uiplatform-foundation';
import type { UseBoundStore, StoreApi } from 'zustand';
import type { AuthSession, AuthState } from '../types';
import { TokenService } from '../services/TokenService';

interface AuthStoreActions {
  setSession: (session: AuthSession) => void;
  clearSession: () => void;
  expireSession: () => void;
  setError: (error: string | null) => void;
  setInitializing: (initializing: boolean) => void;
}

const emptySession: AuthSession = {
  accessToken: null,
  refreshToken: null,
  expiresAt: null,
  sessionId: null,
  user: null,
  claims: null,
};

export const authStore: UseBoundStore<StoreApi<AuthState & AuthStoreActions>> = createStore<
  AuthState & AuthStoreActions
>((set) => ({
  ...emptySession,
  status: 'initializing',
  error: null,
  sessionExpired: false,

  setSession: (session) =>
    set({ ...session, status: session.accessToken ? 'authenticated' : 'unauthenticated', error: null, sessionExpired: false }),

  clearSession: () => {
    TokenService.clear();
    set({ ...emptySession, status: 'unauthenticated', error: null, sessionExpired: false });
  },

  expireSession: () => {
    TokenService.clear();
    set({ ...emptySession, status: 'unauthenticated', error: null, sessionExpired: true });
  },

  setError: (error) => set({ error }),

  setInitializing: (initializing) => set({ status: initializing ? 'initializing' : 'unauthenticated' }),
}));

/**
 * Static accessor for the current access token, independent of React.
 * Wire this into Foundation's AppConfig.getAuthToken at app bootstrap:
 *   <AppProvider config={{ getAuthToken: getAccessToken }}>
 * Deliberately module-level (not through configureApp here) so UIPlatform.Auth never overwrites
 * other AppConfig overrides the consuming app has already set.
 */
export function getAccessToken(): string | null {
  return authStore.getState().accessToken;
}
