/**
 * ASSUMPTION BOUNDARY.
 * The uploaded APIPlatform.Authentication package includes AuthenticationRequest/Response
 * models and IRefreshTokenService/ISessionService interfaces, but no HTTP controller or
 * refresh/logout endpoint DTOs. The shapes below are inferred from those service interfaces
 * and are NOT confirmed against a real controller. Confirm against the actual endpoint
 * contracts once APIPlatform's HTTP layer for Authentication is available, then delete this
 * file's guesses in favor of real shared types (ideally sourced from Nucleus.SharedSchema).
 */

/** Guessed request body for POST {refreshPath}. */
export interface RefreshTokenRequest {
  refreshToken: string;
}

/** Guessed request body for POST {logoutPath}. */
export interface LogoutRequest {
  refreshToken?: string;
  sessionId?: string;
}
