import { apiRequest, getApiClient, getAppConfig } from '@nucleus/uiplatform-foundation';
import { getAuthConfig } from '../config/authConfig';
import type { AuthenticationResponse, LoginCredentials } from '../types';
import type { LogoutRequest, RefreshTokenRequest } from '../types/assumptions';

/**
 * Thin HTTP layer over APIPlatform.Authentication's endpoints. No token storage, no React —
 * pure request/response. Storage is TokenService's job; orchestration is AuthContext's job.
 */
class AuthServiceImpl {
  async login(credentials: LoginCredentials): Promise<AuthenticationResponse> {
    const client = getApiClient(getAppConfig());
    const { loginPath } = getAuthConfig();

    return apiRequest<AuthenticationResponse>(client, {
      method: 'POST',
      url: loginPath,
      data: {
        loginIdentifier: credentials.loginIdentifier,
        password: credentials.password,
        tenantId: credentials.tenantId,
        applicationId: credentials.applicationId,
      },
    });
  }

  async refresh(refreshToken: string, userId: string): Promise<AuthenticationResponse> {
    const client = getApiClient(getAppConfig());
    const { refreshPath } = getAuthConfig();

    return apiRequest<AuthenticationResponse>(client, {
      method: 'POST',
      url: refreshPath,
      data: { refreshToken, userId } satisfies RefreshTokenRequest,
    });
  }

  async logout(request: LogoutRequest): Promise<void> {
    const client = getApiClient(getAppConfig());
    const { logoutPath } = getAuthConfig();

    await apiRequest<void>(client, {
      method: 'POST',
      url: logoutPath,
      data: request,
    });
  }
}

export const AuthService = new AuthServiceImpl();
