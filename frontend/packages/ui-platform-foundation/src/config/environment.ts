import type { EnvironmentName } from '../types';

/**
 * Reads a Vite env variable safely, with an optional fallback.
 * Consuming apps define VITE_* variables in their own .env files;
 * Foundation never assumes which ones exist beyond the base set below.
 */
export function readEnvVar(key: string, fallback = ''): string {
  const value = (import.meta as ImportMeta & { env?: Record<string, string> }).env?.[key];
  return value ?? fallback;
}

export function getEnvironmentName(): EnvironmentName {
  const raw = readEnvVar('VITE_ENVIRONMENT', 'development').toLowerCase();
  if (raw === 'production' || raw === 'staging' || raw === 'test') {
    return raw;
  }
  return 'development';
}

export function isProduction(): boolean {
  return getEnvironmentName() === 'production';
}
