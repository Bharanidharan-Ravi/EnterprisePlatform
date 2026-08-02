import type { ApiResponse } from '../types';
import { ApiError } from '../types';

/**
 * Unwraps a Nucleus ApiResponse<T> envelope, throwing a normalized ApiError on failure.
 * Every Foundation-based request should be passed through this before returning data to callers.
 */
export function unwrapResponse<T>(response: ApiResponse<T>, httpStatus?: number): T {
  if (!response.success) {
    throw new ApiError({
      code: response.error?.code ?? 'UNKNOWN_ERROR',
      message: response.error?.message ?? 'An unknown error occurred.',
      status: httpStatus,
      fieldErrors: response.error?.fieldErrors,
      traceId: response.traceId,
    });
  }
  // data is guaranteed present when success is true by API contract
  return response.data as T;
}
