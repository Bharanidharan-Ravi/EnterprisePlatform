import type { LookupOption, LookupResolver } from '../types';

/**
 * Registry of async lookup resolvers keyed by lookupKey. Forms has zero knowledge of how a
 * lookup is actually fetched (CRUD endpoint, static list, external API) — the consuming app
 * registers a resolver per key; useLookup() just calls resolve().
 */
class LookupServiceImpl {
  private readonly resolvers = new Map<string, LookupResolver>();

  registerResolver(key: string, resolver: LookupResolver): void {
    this.resolvers.set(key, resolver);
  }

  async resolve(key: string, params?: Record<string, unknown>): Promise<LookupOption[]> {
    const resolver = this.resolvers.get(key);
    if (!resolver) {
      throw new Error(`LookupService: no resolver registered for key "${key}". Call LookupService.registerResolver() at app bootstrap.`);
    }
    return resolver(params);
  }

  has(key: string): boolean {
    return this.resolvers.has(key);
  }
}

export const LookupService = new LookupServiceImpl();
