import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import type { ITenantContext } from '../types';
import { setActiveTenantId } from '../config/appConfig';

const TenantContext = createContext<ITenantContext | undefined>(undefined);

/**
 * Provides tenant-scoping to the component tree. Present from day one per the platform's
 * multi-tenancy rule, even though no consuming package resolves a real tenant yet.
 */
export function TenantProvider({
  children,
  initialTenantId = null,
}: {
  children: ReactNode;
  initialTenantId?: string | null;
}) {
  const [tenantId, setTenantId] = useState<string | null>(initialTenantId);

  useEffect(() => {
    setActiveTenantId(tenantId);
  }, [tenantId]);

  const value = useMemo<ITenantContext>(() => ({ tenantId, setTenantId }), [tenantId]);

  return <TenantContext.Provider value={value}>{children}</TenantContext.Provider>;
}

/** Reads the current tenant context. Must be used within a TenantProvider. */
export function useTenant(): ITenantContext {
  const ctx = useContext(TenantContext);
  if (!ctx) {
    throw new Error('useTenant must be used within a TenantProvider');
  }
  return ctx;
}
