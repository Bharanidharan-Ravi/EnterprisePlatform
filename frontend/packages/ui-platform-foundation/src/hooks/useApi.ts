import { useMemo } from 'react';
import type { AxiosRequestConfig } from 'axios';
import { getApiClient, apiRequest } from '../api/apiClient';
import { getAppConfig } from '../config/appConfig';

/**
 * Returns a typed request function bound to the app's shared, configured axios client.
 * This is the low-level primitive other Foundation hooks (useQuery/useMutation) build on.
 */
export function useApi() {
  return useMemo(() => {
    const config = getAppConfig();
    const client = getApiClient(config);

    return {
      request: <T,>(requestConfig: AxiosRequestConfig) => apiRequest<T>(client, requestConfig),
      client,
    };
  }, []);
}
