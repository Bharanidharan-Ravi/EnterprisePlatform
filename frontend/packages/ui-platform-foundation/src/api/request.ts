import type { QueryParams } from '../types';

/**
 * Converts a generic QueryParams object into a URLSearchParams instance,
 * skipping undefined/null values. Field names are caller-supplied — Foundation
 * has no knowledge of any specific entity's fields.
 */
export function toQueryString(params?: QueryParams): string {
  if (!params) return '';
  const search = new URLSearchParams();

  Object.entries(params).forEach(([key, value]) => {
    if (value === undefined || value === null || value === '') return;
    search.append(key, String(value));
  });

  const query = search.toString();
  return query ? `?${query}` : '';
}
