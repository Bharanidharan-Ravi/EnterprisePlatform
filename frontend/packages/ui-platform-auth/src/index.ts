// Types
export * from './types';

// Config
export { configureAuth, getAuthConfig } from './config/authConfig';

// Contexts
export { AuthProvider, useAuthContext } from './contexts/AuthContext';

// Hooks
export { useAuth, type UseAuthResult } from './hooks/useAuth';
export { useCurrentUser, type CurrentUser } from './hooks/useCurrentUser';
export { usePermission, useAllPermissions, useAnyPermission } from './hooks/usePermission';
export { useRole, useAnyRole } from './hooks/useRole';

// Guards
export { AuthGuard, type AuthGuardProps } from './guards/AuthGuard';
export { PermissionGuard, type PermissionGuardProps } from './guards/PermissionGuard';
export { RoleGuard, type RoleGuardProps } from './guards/RoleGuard';

// Components
export { LoginForm, type LoginFormProps } from './components/LoginForm';
export { LogoutButton, type LogoutButtonProps } from './components/LogoutButton';
export { SessionExpiredDialog, type SessionExpiredDialogProps } from './components/SessionExpiredDialog';
export { LoadingScreen, type LoadingScreenProps } from './components/LoadingScreen';

// Services (rarely needed directly — most consumers should use hooks instead)
export { AuthService } from './services/AuthService';
export { TokenService } from './services/TokenService';
export { SessionService } from './services/SessionService';

// Stores — getAccessToken is the key export: wire it into Foundation's
// <AppProvider config={{ getAuthToken: getAccessToken }}> at app bootstrap.
export { authStore, getAccessToken } from './stores/authStore';

/**
 * Deliberately NOT exported (internal implementation detail):
 * - useAuthContext is exported for advanced cases, but useAuth() is the intended public surface.
 */
