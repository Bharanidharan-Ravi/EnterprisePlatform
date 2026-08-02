// Types
export * from './types';

// Config
export { configureApp, getAppConfig } from './config/appConfig';
export { getEnvironmentName, isProduction, readEnvVar } from './config/environment';

// API
export { createApiClient, getApiClient, resetApiClient, apiRequest } from './api/apiClient';
export { unwrapResponse } from './api/response';
export { toQueryString } from './api/request';

// Contexts
export { TenantProvider, useTenant } from './contexts/TenantContext';

// Hooks
export { useApi } from './hooks/useApi';
export { useApiQuery, type UseApiQueryOptions } from './hooks/useQuery';
export { useApiMutation, type UseApiMutationOptions } from './hooks/useMutation';

// Providers
export { AppProvider, type AppProviderProps } from './providers/AppProvider';
export { QueryProvider } from './providers/QueryProvider';
export { AppRouterProvider } from './providers/RouterProvider';

// Errors
export { AppErrorBoundary, type AppErrorBoundaryProps } from './errors/AppErrorBoundary';

// Logging (extension point only — no implementation)
export { getLogger, setLogger, noopLogger } from './services/logger';

// Stores
export { createStore } from './stores/createStore';

// Constants
export { HttpStatus, type HttpStatusCode } from './constants/httpStatus';

// Utils
export { isApiError, toErrorMessage } from './utils/guards';

/**
 * Deliberately NOT exported (internal implementation detail, may change without notice):
 * - setActiveTenantId / getActiveTenantId (config/appConfig.ts) — internal tenant bridge;
 *   consumers must go through TenantProvider/useTenant instead.
 * - attachRequestInterceptor / attachErrorInterceptor (api/interceptors.ts) — internal wiring
 *   used by createApiClient; not a public extension point.
 * - isRecord (utils/guards.ts) — internal type-narrowing helper, not a public contract.
 */
