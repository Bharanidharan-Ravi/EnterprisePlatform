import { useApiMutation, isApiError, type UseApiMutationOptions } from '@nucleus/uiplatform-foundation';
import { authStore } from '@nucleus/uiplatform-auth';
import type { UseMutationResult } from '@tanstack/react-query';

/**
 * Manual probes for the two auth-protected endpoints, plus the live session/expiry the app is
 * currently holding. Exists to make the silent-refresh cycle (see main.tsx's
 * silentRefreshLeadMinutes/ExpiryMinutes test config) visible without opening devtools: the
 * "Session" line changes roughly once a minute as AuthProvider silently refreshes in the
 * background, and both calls below keep succeeding across that swap — proving the *new* token
 * works and (via AuthenticationExecutor's session-ordering fix) the *old* one no longer does,
 * since its session was revoked the moment the refresh that replaced it ran.
 */
export function AuthDebugPanel() {
  const sessionId = authStore((s) => s.sessionId);
  const expiresAt = authStore((s) => s.expiresAt);

  const me = useEndpointCall('/auth/me');
  const protectedCall = useEndpointCall('/auth/protected');

  return (
    <section className="auth-debug-panel">
      <h2>Auth debug</h2>
      <p className="hint">
        Session <code>{sessionId ? `${sessionId.slice(0, 12)}…` : 'none'}</code>, access token expires{' '}
        <code>{expiresAt ? new Date(expiresAt).toLocaleTimeString() : '—'}</code>. Refreshed silently in
        the background roughly once a minute (test-only cadence) — watch these values change and re-run
        the calls below to confirm the new token works.
      </p>

      <div className="auth-debug-row">
        <button onClick={() => me.mutate()} disabled={me.isPending}>
          Call GET /auth/me
        </button>
        <ResultBadge mutation={me} />
      </div>

      <div className="auth-debug-row">
        <button onClick={() => protectedCall.mutate()} disabled={protectedCall.isPending}>
          Call GET /auth/protected
        </button>
        <ResultBadge mutation={protectedCall} />
      </div>
    </section>
  );
}

function useEndpointCall(url: string) {
  const options: UseApiMutationOptions<unknown, void> = { buildRequest: () => ({ url, method: 'GET' }) };
  return useApiMutation<unknown, void>(options);
}

function ResultBadge({ mutation }: { mutation: UseMutationResult<unknown, Error, void> }) {
  if (mutation.isPending) return <span>…</span>;

  if (mutation.isError) {
    const err = mutation.error;
    return (
      <span className="form-error">
        {isApiError(err) ? `${err.status ?? '?'} ${err.code}: ${err.message}` : 'Request failed.'}
      </span>
    );
  }

  if (mutation.isSuccess) {
    return <pre>{JSON.stringify(mutation.data, null, 2)}</pre>;
  }

  return <span className="hint">not called yet</span>;
}
