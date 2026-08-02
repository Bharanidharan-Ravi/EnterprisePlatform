import type { ReactNode } from 'react';
import { useRole, useAnyRole } from '../hooks/useRole';

export interface RoleGuardProps {
  children: ReactNode;
  /** Single role id to require. Ignored if `any` is provided. */
  role?: string;
  /** Require at least one of these role ids. */
  any?: string[];
  fallback?: ReactNode;
}

/** Renders children only if the current user holds the required role(s). UX-level gate only. */
export function RoleGuard({ children, role, any, fallback = null }: RoleGuardProps) {
  const singleOk = useRole(role ?? '__none__');
  const anyOk = useAnyRole(any ?? []);

  const allowed = any?.length ? anyOk : Boolean(role) && singleOk;

  return allowed ? <>{children}</> : <>{fallback}</>;
}
