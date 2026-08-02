import { useQuery } from '@tanstack/react-query';
import { LookupService } from '../services/LookupService';
import type { LookupOption } from '../types';

/** Fetches (and caches) options for a registered lookup key. Pass `params` for dependent lookups. */
export function useLookup(lookupKey: string | undefined, params?: Record<string, unknown>) {
  return useQuery<LookupOption[]>({
    queryKey: ['nucleus-forms-lookup', lookupKey, params],
    queryFn: () => LookupService.resolve(lookupKey as string, params),
    enabled: Boolean(lookupKey),
    staleTime: 60_000,
  });
}
