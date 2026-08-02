import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useState, type ReactNode } from 'react';

export interface QueryProviderProps {
  children: ReactNode;
  /** Optional pre-built QueryClient, e.g. for tests that need to inspect/seed the cache. */
  client?: QueryClient;
}

/** Wraps the app with a TanStack QueryClientProvider using sensible, generic defaults. */
export function QueryProvider({ children, client }: QueryProviderProps) {
  const [queryClient] = useState(
    () =>
      client ??
      new QueryClient({
        defaultOptions: {
          queries: {
            retry: 1,
            refetchOnWindowFocus: false,
            staleTime: 30_000,
          },
        },
      }),
  );

  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
}
