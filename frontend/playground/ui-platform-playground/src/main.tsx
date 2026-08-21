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
 * ('/auth/refresh-token') doesn't match the actual backend route ('/auth/refresh'). This does
 * NOT make refresh functional end-to-end — AuthenticationService.RefreshAsync still always
 * returns Ok=false by platform design (documented known limitation, phase2 report Section N) —
 * it only stops the call from 404ing needlessly.
 */
configureAuth({ refreshPath: '/auth/refresh' });

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AppProvider
      config={{
        apiBaseUrl: 'http://localhost:5099/api',
        getAuthToken: getAccessToken,
      }}
    >
      <AuthProvider>
        <App />
      </AuthProvider>
    </AppProvider>
  </StrictMode>,
);
