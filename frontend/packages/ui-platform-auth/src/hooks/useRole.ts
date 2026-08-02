import { authStore } from '../stores/authStore';

/** Checks whether the current user holds a given role id (APIPlatform.Rbac.Models.Role.Id). */
export function useRole(roleId: string): boolean {
  const roles = authStore((s) => s.claims?.role ?? []);
  return roles.includes(roleId);
}

/** Checks whether the current user holds ANY of the given role ids. */
export function useAnyRole(roleIds: string[]): boolean {
  const roles = authStore((s) => s.claims?.role ?? []);
  return roleIds.some((id) => roles.includes(id));
}
