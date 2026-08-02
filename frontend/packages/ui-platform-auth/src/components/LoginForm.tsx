import { useState, type FormEvent } from 'react';
import { useAuth } from '../hooks/useAuth';

export interface LoginFormProps {
  onSuccess?: () => void;
  tenantId?: string;
  applicationId?: string;
  /** Show a "remember me" checkbox. Defaults to true. */
  allowRememberMe?: boolean;
}

/** Minimal, unopinionated login form. Consuming apps restyle/wrap as needed — no branding here. */
export function LoginForm({ onSuccess, tenantId, applicationId, allowRememberMe = true }: LoginFormProps) {
  const { login, error } = useAuth();
  const [loginIdentifier, setLoginIdentifier] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitting(true);
    try {
      await login({ loginIdentifier, password, tenantId, applicationId, rememberMe });
      onSuccess?.();
    } catch {
      // error is surfaced via useAuth().error
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-3">
      <label className="flex flex-col gap-1">
        <span className="text-sm font-medium">Username or email</span>
        <input
          type="text"
          value={loginIdentifier}
          onChange={(e) => setLoginIdentifier(e.target.value)}
          autoComplete="username"
          required
          className="border rounded px-3 py-2"
        />
      </label>

      <label className="flex flex-col gap-1">
        <span className="text-sm font-medium">Password</span>
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          autoComplete="current-password"
          required
          className="border rounded px-3 py-2"
        />
      </label>

      {allowRememberMe && (
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={rememberMe} onChange={(e) => setRememberMe(e.target.checked)} />
          Remember me
        </label>
      )}

      {error && <p className="text-sm text-red-600">{error}</p>}

      <button type="submit" disabled={submitting} className="rounded px-3 py-2 bg-blue-600 text-white disabled:opacity-50">
        {submitting ? 'Signing in…' : 'Sign in'}
      </button>
    </form>
  );
}
