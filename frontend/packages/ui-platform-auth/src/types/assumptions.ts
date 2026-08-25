/**
 * ASSUMPTION BOUNDARY.
 * The uploaded APIPlatform.Authentication package includes AuthenticationRequest/Response
 * models and IRefreshTokenService/ISessionService interfaces, but no HTTP controller or
 * refresh/logout endpoint DTOs. The shapes below are inferred from those service interfaces
 * and are NOT confirmed against a real controller. Confirm against the actual endpoint
 * contracts once APIPlatform's HTTP layer for Authentication is available, then delete this
 * file's guesses in favor of real shared types (ideally sourced from Nucleus.SharedSchema).
 */

/**
 * Request body for POST {refreshPath}. Confirmed against the real controller
 * (APIPlatform.Playground.Controllers.RefreshRequest) — userId is required there; omitting it
 * 400s ("missing required properties including: 'userId'"), which every silent refresh hit until
 * this was added, since AuthContext's performRefresh only ever sent refreshToken. A failed refresh
 * is treated as "session over" (see AuthContext.performRefresh's catch -> expireSession()), so this
 * was surfacing as an unexplained auto-logout roughly once per refresh cycle.
 */
export interface RefreshTokenRequest {
  refreshToken: string;
  userId: string;
}

/** Guessed request body for POST {logoutPath}. */
export interface LogoutRequest {
  refreshToken?: string;
  sessionId?: string;
}
