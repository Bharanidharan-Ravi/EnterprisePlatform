/**
 * Shared, generic types used across every Nucleus UIPlatform package.
 * No domain/business concepts belong here — only engineering primitives.
 */
import type { ComponentType, ReactNode } from 'react';

/** Standard envelope every Nucleus API endpoint returns. */
export interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  error: ApiErrorDetail | null;
  /** Correlation id for tracing a request across API + logs. */
  traceId?: string;
}

export interface ApiErrorDetail {
  code: string;
  message: string;
  /** Field-level validation errors, if any. Keys are field names. */
  fieldErrors?: Record<string, string[]>;
}

/** Normalized error thrown by the API client on failure. */
export class ApiError extends Error {
  public readonly code: string;
  public readonly status?: number;
  public readonly fieldErrors?: Record<string, string[]>;
  public readonly traceId?: string;

  constructor(params: {
    code: string;
    message: string;
    status?: number;
    fieldErrors?: Record<string, string[]>;
    traceId?: string;
  }) {
    super(params.message);
    this.name = 'ApiError';
    this.code = params.code;
    this.status = params.status;
    this.fieldErrors = params.fieldErrors;
    this.traceId = params.traceId;
  }
}

/** Generic paged result shape returned by any list endpoint. */
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/** Generic paging/sorting/filtering request shape. Field names are supplied by callers. */
export interface QueryParams {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  search?: string;
  [key: string]: unknown;
}

/**
 * Minimal tenant-context contract the UI layer depends on.
 * Multi-tenancy is a first-class concern from day one (mirrors ITenantContext on the API side),
 * even though no consuming package uses it yet.
 */
export interface ITenantContext {
  tenantId: string | null;
  setTenantId(tenantId: string | null): void;
}

/** Supported deployment environments. */
export type EnvironmentName = 'development' | 'staging' | 'production' | 'test';

/** Severity levels for the logging extension point. */
export type LogLevel = 'debug' | 'info' | 'warn' | 'error';

/**
 * Diagnostics extension point. Foundation ships only a no-op default (see services/logger.ts);
 * a future UIPlatform.Logging (or similar) package plugs in a real implementation via setLogger().
 */
export interface Logger {
  debug(message: string, context?: Record<string, unknown>): void;
  info(message: string, context?: Record<string, unknown>): void;
  warn(message: string, context?: Record<string, unknown>): void;
  error(message: string, error?: unknown, context?: Record<string, unknown>): void;
}

/**
 * Shape of a future ThemeProvider component. AppProvider accepts one via this contract
 * so a future UIPlatform.Theme package can plug in without changing AppProvider's signature.
 * Foundation defines no theming behavior itself.
 */
export type ThemeProviderComponent = ComponentType<{ children: ReactNode }>;

/** Application-wide configuration contract. Extended, not modified, by consuming packages. */
export interface AppConfig {
  apiBaseUrl: string;
  environment: EnvironmentName;
  /** Optional resolver invoked before every request to attach a tenant header. */
  getTenantId?: () => string | null;
  /** Optional resolver invoked before every request to attach an auth token. Implemented by UIPlatform.Auth. */
  getAuthToken?: () => string | null | Promise<string | null>;
  defaultTimeoutMs?: number;
}
