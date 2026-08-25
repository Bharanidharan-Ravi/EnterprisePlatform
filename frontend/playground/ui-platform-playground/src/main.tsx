import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { AppProvider } from '@nucleus/uiplatform-foundation';
import { AuthProvider, configureAuth, getAccessToken } from '@nucleus/uiplatform-auth';
import { App } from './App';
import './styles.css';

/**
 * Phase 2 bootstrap — the first real consumer of AppProvider + AuthProvider together.
 *
 * configureAuth's refreshPath override fixes a confirmed mismatch: ui-platform-auth's default
 * ('/auth/refresh-token') doesn't match the actual backend route ('/auth/refresh'). Refresh is now
 * fully functional end-to-end (AuthenticationExecutor session-ordering fix + real
 * AuthenticationService.RefreshAsync) — the earlier "always returns Ok=false" note here was stale.
 *
 * silentRefreshLeadMinutes: 1 is a TEST-ONLY cadence, paired with this host's
 * appsettings.json Authentication:Jwt:ExpiryMinutes: 2 (also test-only; a real deployment would use
 * something like 60/2). AuthProvider's existing SessionService.schedule() fires a silent refresh
 * (expiry - lead) after every token it applies, and re-schedules itself on each new token via the
 * authStore subscription — so with a 2-minute token and a 1-minute lead, this alone produces a
 * silent refresh roughly once a minute, forever, with no separate timer needed. Each refresh mints a
 * new session (and revokes the old one — see AuthenticationExecutor's session-ordering fix), so the
 * previous access token stops authenticating immediately rather than staying valid until its own
 * expiry. Use the AuthDebugPanel buttons on the Employee page to watch that happen.
 */
configureAuth({ refreshPath: '/auth/refresh', silentRefreshLeadMinutes: 1 });

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AppProvider
      config={{
        apiBaseUrl: 'http://localhost:5000/api',
        getAuthToken: getAccessToken,
      }}
    >
      <AuthProvider>
        <App />
      </AuthProvider>
    </AppProvider>
  </StrictMode>,
);
