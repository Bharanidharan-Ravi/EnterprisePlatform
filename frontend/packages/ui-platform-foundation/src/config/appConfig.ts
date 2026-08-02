import type { AppConfig } from '../types';
import { getEnvironmentName, readEnvVar } from './environment';

let currentConfig: AppConfig | null = null;
let activeTenantId: string | null = null;

/**
 * Module-level tenant accessor bridged from TenantProvider so non-React code
 * (e.g. axios interceptors) can read the current tenant without prop drilling.
 */
export function setActiveTenantId(tenantId: string | null): void {
  activeTenantId = tenantId;
}

export function getActiveTenantId(): string | null {
  return activeTenantId;
}

/** Builds a default config from Vite env vars. Consuming apps may override any field via configureApp(). */
function buildDefaultConfig(): AppConfig {
  return {
    apiBaseUrl: readEnvVar('VITE_API_BASE_URL', '/api'),
    environment: getEnvironmentName(),
    defaultTimeoutMs: 30_000,
    getTenantId: () => activeTenantId,
  };
}

/**
 * Configures the app-wide settings used by the API client and other Foundation services.
 * Must be called once, before rendering (typically in main.tsx), by every consuming app.
 */
export function configureApp(overrides: Partial<AppConfig> = {}): AppConfig {
  currentConfig = { ...buildDefaultConfig(), ...overrides };
  return currentConfig;
}

/** Returns the active AppConfig, configuring with defaults on first access if needed. */
export function getAppConfig(): AppConfig {
  if (!currentConfig) {
    currentConfig = buildDefaultConfig();
  }
  return currentConfig;
}
