import { BrowserRouter, type BrowserRouterProps } from 'react-router-dom';
import type { ReactNode } from 'react';

export interface RouterProviderProps extends BrowserRouterProps {
  children: ReactNode;
}

/**
 * Base routing provider. Foundation only establishes the router itself —
 * route guards, protected routes, etc. are added by UIPlatform.Auth.
 */
export function AppRouterProvider({ children, ...routerProps }: RouterProviderProps) {
  return <BrowserRouter {...routerProps}>{children}</BrowserRouter>;
}
