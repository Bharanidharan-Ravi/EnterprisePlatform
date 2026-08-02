/**
 * Shared types for UIPlatform.Auth.
 * Mirrors APIPlatform.Authentication's AuthenticationRequest/Response contracts and the
 * role/permission claims embedded by ClaimsBuilder (APIPlatform.Authentication.Claims).
 * No business/domain concepts belong here.
 */

/** Credentials submitted to APIPlatform.Authentication's login endpoint. */
export interface LoginCredentials {
  loginIdentifier: string;
  password: string;
  tenantId?: string;
  applicationId?: string;
  /** If true, refresh token persists in localStorage across browser restarts; otherwise sessionStorage. */
  rememberMe?: boolean;
}

/** Mirrors APIPlatform.Authentication.Models.UserProfile (the public-facing user shape). */
export interface AuthUser {
  userId: string;
  username: string;
  email?: string;
  roleIds: string[];
}

/** Mirrors APIPlatform.Authentication.Models.AuthenticationResponse. */
export interface AuthenticationResponse {
  ok: boolean;
  accessToken?: string;
  refreshToken?: string;
  expiresAt?: string;
  sessionId?: string;
  user?: AuthUser;
  errorCode?: string;
  errorMessage?: string;
}

/** Claims decoded from the JWT access token, as embedded by ClaimsBuilder. */
export interface DecodedAccessToken {
  sub: string;
  /** ClaimTypes.Role — may appear once or repeated per role. */
  role: string[];
  /** "permission" claim — one entry per PermissionKey (APIPlatform.Rbac.Models.Permission.Key). */
  permission: string[];
  tenantId?: string;
  companyId?: string;
  branchId?: string;
  departmentId?: string;
  sessionId?: string;
  /** Unix seconds expiry (JWT "exp"). */
  exp: number;
  /** Any additional claims (ExtendedClaims, IClaimsBuilderExtension output) not modeled above. */
  extra: Record<string, string>;
}

/** Persisted/in-memory auth session state. */
export interface AuthSession {
  accessToken: string | null;
  refreshToken: string | null;
  expiresAt: string | null;
  sessionId: string | null;
  user: AuthUser | null;
  claims: DecodedAccessToken | null;
}

export type AuthStatus = 'initializing' | 'authenticated' | 'unauthenticated';

export interface AuthState extends AuthSession {
  status: AuthStatus;
  error: string | null;
  /** True when the session ended due to expiry/failed silent refresh, as opposed to explicit logout. */
  sessionExpired: boolean;
}

/** Configurable endpoints/behavior — config is the source of truth, never hardcoded. */
export interface AuthConfig {
  loginPath: string;
  refreshPath: string;
  logoutPath: string;
  /** Route to redirect to when AuthGuard denies access. */
  loginRedirectPath: string;
  /** Minutes before token expiry to proactively attempt silent refresh. */
  silentRefreshLeadMinutes: number;
}
