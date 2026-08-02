import { ApiError } from '../types';

/** Type guard: is this error a Foundation ApiError? */
export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError;
}

/** Narrows an unknown value to a non-null object. */
export function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

/** Returns a human-readable message for any thrown value, falling back gracefully. */
export function toErrorMessage(error: unknown, fallback = 'An unexpected error occurred.'): string {
  if (isApiError(error)) return error.message;
  if (error instanceof Error) return error.message;
  if (typeof error === 'string') return error;
  return fallback;
}
