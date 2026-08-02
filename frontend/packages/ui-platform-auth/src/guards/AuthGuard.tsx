import { Navigate, Outlet, useLocation } from 'react-router-dom';
import type { ReactNode } from 'react';
import { useAuth } from '../hooks/useAuth';
import { getAuthConfig } from '../config/authConfig';
import { LoadingScreen } from '../components/LoadingScreen';

export interface AuthGuardProps {
  /** Renders as a layout wrapper (via <Outlet/>) when omitted, or wraps explicit children. */
  children?: ReactNode;
  redirectTo?: string;
}

/** Blocks access until authenticated. Redirects to loginRedirectPath (or override), preserving the attempted location. */
export function AuthGuard({ children, redirectTo }: AuthGuardProps) {
  const { isAuthenticated, isInitializing } = useAuth();
  const location = useLocation();

  if (isInitializing) return <LoadingScreen />;

  if (!isAuthenticated) {
    const target = redirectTo ?? getAuthConfig().loginRedirectPath;
    return <Navigate to={target} state={{ from: location }} replace />;
  }

  return children ? <>{children}</> : <Outlet />;
}
