import { useState } from 'react';
import { isApiError } from '@nucleus/uiplatform-foundation';
import { useAuth, LogoutButton, PermissionGuard } from '@nucleus/uiplatform-auth';
import { useEmployeeList, useDeleteEmployee } from './employeeApi';
import { EmployeeForm } from './EmployeeForm';
import { AuthDebugPanel } from '../debug/AuthDebugPanel';
import type { Employee } from './types';

/**
 * ============================================================================
 * Application test UI — NOT UIPlatform Grid (phase2.md 30). There is no UI Grid
 * package yet; this is a deliberately plain HTML table, scoped to this test app only.
 * ============================================================================
 */
export function EmployeeListPage() {
  const { user } = useAuth();
  const [employeeCodeFilter, setEmployeeCodeFilter] = useState('');
  const [sort, setSort] = useState('name');
  const [editing, setEditing] = useState<Employee | 'new' | null>(null);

  const { data: employees, isLoading, error, refetch } = useEmployeeList({
    employeeCode: employeeCodeFilter || undefined,
    sort,
    page: 1,
    pageSize: 20,
  });
  const deleteEmployee = useDeleteEmployee();

  if (editing) {
    return (
      <EmployeeForm
        editing={editing === 'new' ? undefined : editing}
        onDone={() => {
          setEditing(null);
          refetch();
        }}
        onCancel={() => setEditing(null)}
      />
    );
  }

  return (
    <div className="employee-list-page">
      <header>
        <h1>Employees (Phase 2 — Application test UI, NOT UIPlatform Grid)</h1>
        <div>
          Signed in as <strong>{user?.username}</strong> <LogoutButton />
        </div>
      </header>

      <AuthDebugPanel />

      <div className="toolbar">
        <input
          placeholder="Filter by Employee Code"
          value={employeeCodeFilter}
          onChange={(e) => setEmployeeCodeFilter(e.target.value)}
        />
        <select value={sort} onChange={(e) => setSort(e.target.value)}>
          <option value="name">Name ↑</option>
          <option value="-name">Name ↓</option>
        </select>
        {/* UX-level gate only (see PermissionGuard's own doc comment) — the API pipeline
            (ICrudAuthorizationService, called by EmployeesController.Create) is the real
            authority and rejects this independently of what the UI shows. */}
        <PermissionGuard permission="employee.create">
          <button onClick={() => setEditing('new')}>New Employee</button>
        </PermissionGuard>
      </div>

      {isLoading && <p>Loading…</p>}
      {error && <p className="form-error">{isApiError(error) ? error.message : 'Failed to load employees.'}</p>}

      {employees && (
        <table>
          <thead>
            <tr>
              <th>Code</th>
              <th>Name</th>
              <th>Email</th>
              <th>Department</th>
              <th>Active</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {employees.map((emp) => (
              <tr key={emp.id}>
                <td>{emp.employeeCode}</td>
                <td>{emp.name}</td>
                <td>{emp.email}</td>
                <td>{emp.department ?? '—'}</td>
                <td>{emp.isActive ? 'Yes' : 'No'}</td>
                <td>
                  <PermissionGuard permission="employee.update">
                    <button onClick={() => setEditing(emp)}>Edit</button>
                  </PermissionGuard>
                  <PermissionGuard permission="employee.delete">
                    <button
                      onClick={async () => {
                        try {
                          await deleteEmployee.mutateAsync(emp.id);
                          refetch();
                        } catch {
                          // surfaced via deleteEmployee.error below
                        }
                      }}
                    >
                      Delete
                    </button>
                  </PermissionGuard>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {deleteEmployee.isError && (
        <p className="form-error">
          {isApiError(deleteEmployee.error) ? deleteEmployee.error.message : 'Delete failed.'}
        </p>
      )}
    </div>
  );
}
