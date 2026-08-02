import { authStore } from '../stores/authStore';
import type { AuthUser } from '../types';

export interface CurrentUser extends AuthUser {
  tenantId?: string;
  companyId?: string;
  branchId?: string;
  departmentId?: string;
}

/** Returns the current user enriched with tenant/company/branch/department claims, or null. */
export function useCurrentUser(): CurrentUser | null {
  const user = authStore((s) => s.user);
  const claims = authStore((s) => s.claims);

  if (!user) return null;

  return {
    ...user,
    tenantId: claims?.tenantId,
    companyId: claims?.companyId,
    branchId: claims?.branchId,
    departmentId: claims?.departmentId,
  };
}
