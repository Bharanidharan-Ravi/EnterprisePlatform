import type { AuthConfig } from '../types';

const defaults: AuthConfig = {
  loginPath: '/auth/login',
  refreshPath: '/auth/refresh-token',
  logoutPath: '/auth/logout',
  loginRedirectPath: '/login',
  silentRefreshLeadMinutes: 2,
};

let currentConfig: AuthConfig = { ...defaults };

/** Overrides auth endpoints/behavior. Call once at app bootstrap, alongside Foundation's configureApp(). */
export function configureAuth(overrides: Partial<AuthConfig> = {}): AuthConfig {
  currentConfig = { ...defaults, ...overrides };
  return currentConfig;
}

export function getAuthConfig(): AuthConfig {
  return currentConfig;
}
