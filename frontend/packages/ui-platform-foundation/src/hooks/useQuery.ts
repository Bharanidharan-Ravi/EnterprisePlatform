import { useQuery as useTanStackQuery, type UseQueryOptions, type UseQueryResult } from '@tanstack/react-query';
import type { AxiosRequestConfig } from 'axios';
import { useApi } from './useApi';

export interface UseApiQueryOptions<T>
  extends Omit<UseQueryOptions<T, Error, T, readonly unknown[]>, 'queryKey' | 'queryFn'> {
  queryKey: readonly unknown[];
  requestConfig: AxiosRequestConfig;
}

/**
 * Generic read hook: fetches data via the Foundation API client and unwraps the envelope.
 * Callers supply their own queryKey and requestConfig — no entity/field knowledge lives here.
 */
export function useApiQuery<T>({ queryKey, requestConfig, ...options }: UseApiQueryOptions<T>): UseQueryResult<T, Error> {
  const { request } = useApi();

  return useTanStackQuery({
    queryKey,
    queryFn: () => request<T>(requestConfig),
    ...options,
  });
}
