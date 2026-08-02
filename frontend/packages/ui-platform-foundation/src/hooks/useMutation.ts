import { useMutation as useTanStackMutation, type UseMutationOptions, type UseMutationResult } from '@tanstack/react-query';
import type { AxiosRequestConfig } from 'axios';
import { useApi } from './useApi';

export interface UseApiMutationOptions<TData, TVariables>
  extends Omit<UseMutationOptions<TData, Error, TVariables>, 'mutationFn'> {
  /** Builds the request config for a given set of variables. */
  buildRequest: (variables: TVariables) => AxiosRequestConfig;
}

/**
 * Generic write hook: performs a mutation via the Foundation API client and unwraps the envelope.
 * No knowledge of the target entity/endpoint lives here — callers supply buildRequest.
 */
export function useApiMutation<TData, TVariables = void>({
  buildRequest,
  ...options
}: UseApiMutationOptions<TData, TVariables>): UseMutationResult<TData, Error, TVariables> {
  const { request } = useApi();

  return useTanStackMutation({
    mutationFn: (variables: TVariables) => request<TData>(buildRequest(variables)),
    ...options,
  });
}
