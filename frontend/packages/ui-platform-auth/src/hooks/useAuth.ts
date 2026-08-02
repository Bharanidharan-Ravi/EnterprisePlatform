import { authStore } from '../stores/authStore';
import { useAuthContext } from '../contexts/AuthContext';
import type { AuthUser, LoginCredentials } from '../types';

export interface UseAuthResult {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isInitializing: boolean;
  error: string | null;
  login: (credentials: LoginCredentials) => Promise<void>;
  logout: () => Promise<void>;
}

/** Primary hook for auth state + actions. Requires an ancestor AuthProvider. */
export function useAuth(): UseAuthResult {
  const status = authStore((s) => s.status);
  const user = authStore((s) => s.user);
  const error = authStore((s) => s.error);
  const { login, logout } = useAuthContext();

  return {
    user,
    isAuthenticated: status === 'authenticated',
    isInitializing: status === 'initializing',
    error,
    login,
    logout,
  };
}
