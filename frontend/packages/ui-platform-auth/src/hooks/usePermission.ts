import { authStore } from '../stores/authStore';

/**
 * Checks whether the current user holds a given permission key (APIPlatform.Rbac.Models.Permission.Key,
 * e.g. "Widget.Read"). Reads from JWT "permission" claims — no extra API round-trip.
 * Note: this is a UX-level gate only; the API pipeline (APIPlatform.Rbac) is the authority.
 */
export function usePermission(permissionKey: string): boolean {
  const permissions = authStore((s) => s.claims?.permission ?? []);
  return permissions.includes(permissionKey);
}

/** Checks whether the current user holds ALL of the given permission keys. */
export function useAllPermissions(permissionKeys: string[]): boolean {
  const permissions = authStore((s) => s.claims?.permission ?? []);
  return permissionKeys.every((key) => permissions.includes(key));
}

/** Checks whether the current user holds ANY of the given permission keys. */
export function useAnyPermission(permissionKeys: string[]): boolean {
  const permissions = authStore((s) => s.claims?.permission ?? []);
  return permissionKeys.some((key) => permissions.includes(key));
}
