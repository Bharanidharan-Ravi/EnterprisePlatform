import type { ReactNode } from 'react';
import { usePermission, useAllPermissions, useAnyPermission } from '../hooks/usePermission';

export interface PermissionGuardProps {
  children: ReactNode;
  /** Single permission key to require. Ignored if `any` or `all` is provided. */
  permission?: string;
  /** Require at least one of these permission keys. */
  any?: string[];
  /** Require all of these permission keys. */
  all?: string[];
  /** Rendered instead of children when the check fails. Defaults to null. */
  fallback?: ReactNode;
}

/** Renders children only if the current user holds the required permission(s). UX-level gate only. */
export function PermissionGuard({ children, permission, any, all, fallback = null }: PermissionGuardProps) {
  const singleOk = usePermission(permission ?? '__none__');
  const anyOk = useAnyPermission(any ?? []);
  const allOk = useAllPermissions(all ?? []);

  let allowed = false;
  if (all?.length) allowed = allOk;
  else if (any?.length) allowed = anyOk;
  else if (permission) allowed = singleOk;

  return allowed ? <>{children}</> : <>{fallback}</>;
}
