import type { ReactNode } from 'react';
import { QueryClient } from '@tanstack/react-query';
import { QueryProvider } from './QueryProvider';
import { AppRouterProvider } from './RouterProvider';
import { TenantProvider } from '../contexts/TenantContext';
import { configureApp } from '../config/appConfig';
import type { AppConfig, ThemeProviderComponent } from '../types';

export interface AppProviderProps {
  children: ReactNode;
  /** Overrides applied on top of env-derived defaults. Call once at the app root. */
  config?: Partial<AppConfig>;
  initialTenantId?: string | null;
  queryClient?: QueryClient;
  /**
   * Extension point for a future theming package (e.g. UIPlatform.Theme).
   * Foundation implements no theming itself — when omitted, children render unwrapped.
   */
  themeProvider?: ThemeProviderComponent;
}

/**
 * Composition root for every Foundation-level provider. Consuming apps mount this once
 * at the top of their tree; feature packages (Auth, Forms, Theme, etc.) add their own providers inside it.
 */
export function AppProvider({
  children,
  config,
  initialTenantId = null,
  queryClient,
  themeProvider: ThemeProvider,
}: AppProviderProps) {
  configureApp(config);

  const themed = ThemeProvider ? <ThemeProvider>{children}</ThemeProvider> : children;

  return (
    <TenantProvider initialTenantId={initialTenantId}>
      <QueryProvider client={queryClient}>
        <AppRouterProvider>{themed}</AppRouterProvider>
      </QueryProvider>
    </TenantProvider>
  );
}

