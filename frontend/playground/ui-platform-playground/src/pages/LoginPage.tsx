import { useLocation, useNavigate } from 'react-router-dom';
import { LoginForm } from '@nucleus/uiplatform-auth';

/**
 * TEST ONLY credentials (mirrors backend/playground/APIPlatform.Playground/Resolvers/
 * PlaygroundIdentityResolver.cs): admin/Admin@123 (full Employee CRUD) or viewer/Viewer@123
 * (Employee read-only — proves RBAC deny in the UI, per phase2.md 22).
 */
export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname ?? '/';

  return (
    <div className="login-page">
      <h1>UIPlatform Playground — Sign in</h1>
      <p className="hint">
        TEST ONLY: <code>admin</code> / <code>Admin@123</code> (full access) or{' '}
        <code>viewer</code> / <code>Viewer@123</code> (read-only — proves RBAC deny).
      </p>
      <LoginForm onSuccess={() => navigate(from, { replace: true })} />
    </div>
  );
}
