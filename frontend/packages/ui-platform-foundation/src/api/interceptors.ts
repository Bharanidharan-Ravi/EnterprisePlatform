import type { AxiosInstance, AxiosError, InternalAxiosRequestConfig } from 'axios';
import type { ApiResponse, AppConfig } from '../types';
import { ApiError } from '../types';

const TENANT_HEADER = 'X-Tenant-Id';

/**
 * Attaches tenant and (if configured by UIPlatform.Auth) auth headers to every outgoing request.
 * Foundation never implements authentication itself — it only exposes the extension point.
 */
export function attachRequestInterceptor(client: AxiosInstance, config: AppConfig): void {
  client.interceptors.request.use(async (requestConfig: InternalAxiosRequestConfig) => {
    const tenantId = config.getTenantId?.();
    if (tenantId) {
      requestConfig.headers.set(TENANT_HEADER, tenantId);
    }

    const token = await config.getAuthToken?.();
    if (token) {
      requestConfig.headers.set('Authorization', `Bearer ${token}`);
    }

    return requestConfig;
  });
}

/**
 * Normalizes transport-level failures (network errors, timeouts, non-2xx with no envelope)
 * into the same ApiError shape used for envelope-level failures.
 */
export function attachErrorInterceptor(client: AxiosInstance): void {
  client.interceptors.response.use(
    (response) => response,
    (error: AxiosError<ApiResponse<unknown>>) => {
      if (error.response?.data && typeof error.response.data === 'object' && 'success' in error.response.data) {
        const envelope = error.response.data;
        return Promise.reject(
          new ApiError({
            code: envelope.error?.code ?? 'HTTP_ERROR',
            message: envelope.error?.message ?? error.message,
            status: error.response.status,
            fieldErrors: envelope.error?.fieldErrors,
            traceId: envelope.traceId,
          }),
        );
      }

      return Promise.reject(
        new ApiError({
          code: error.code ?? 'NETWORK_ERROR',
          message: error.message || 'A network error occurred.',
          status: error.response?.status,
        }),
      );
    },
  );
}
