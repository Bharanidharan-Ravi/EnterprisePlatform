import axios, { type AxiosInstance, type AxiosRequestConfig } from 'axios';
import type { AppConfig } from '../types';
import { unwrapResponse } from './response';
import { attachErrorInterceptor, attachRequestInterceptor } from './interceptors';
import type { ApiResponse } from '../types';

let sharedClient: AxiosInstance | null = null;

/** Creates a fresh, fully-configured axios instance bound to the given AppConfig. */
export function createApiClient(config: AppConfig): AxiosInstance {
  const client = axios.create({
    baseURL: config.apiBaseUrl,
    timeout: config.defaultTimeoutMs ?? 30_000,
    headers: { 'Content-Type': 'application/json' },
  });

  attachRequestInterceptor(client, config);
  attachErrorInterceptor(client);

  return client;
}

/** Lazily creates (and memoizes) the app-wide shared axios instance. */
export function getApiClient(config: AppConfig): AxiosInstance {
  if (!sharedClient) {
    sharedClient = createApiClient(config);
  }
  return sharedClient;
}

/** Resets the memoized shared client. Primarily useful for tests and hot-reload scenarios. */
export function resetApiClient(): void {
  sharedClient = null;
}

/** Thin typed wrapper: performs a request and unwraps the ApiResponse<T> envelope. */
export async function apiRequest<T>(client: AxiosInstance, requestConfig: AxiosRequestConfig): Promise<T> {
  const response = await client.request<ApiResponse<T>>(requestConfig);
  return unwrapResponse(response.data, response.status);
}
