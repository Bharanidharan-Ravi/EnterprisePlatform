import { Routes, Route } from 'react-router-dom';
import { AuthGuard } from '@nucleus/uiplatform-auth';
import { LoginPage } from './pages/LoginPage';
import { EmployeeListPage } from './employee/EmployeeListPage';

/**
 * Phase 2 end-to-end flow (phase2.md 31): Login -> Authenticated UI -> Employee list/form ->
 * API -> RBAC -> CrudEngine -> SharedSchema -> Dapper -> SQL Server -> Response -> TanStack
 * Query/UI state -> Rendered result.
 */
export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<AuthGuard />}>
        <Route path="/" element={<EmployeeListPage />} />
      </Route>
    </Routes>
  );
}
