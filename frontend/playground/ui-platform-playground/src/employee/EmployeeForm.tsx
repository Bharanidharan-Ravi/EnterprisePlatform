import { useMemo } from 'react';
import { FormService, Form, Field } from '@nucleus/uiplatform-forms';
import { isApiError } from '@nucleus/uiplatform-foundation';
import { employeeSchemaFields } from './employeeSchema';
import { useCreateEmployee, useUpdateEmployee } from './employeeApi';
import type { Employee, EmployeeFormValues } from './types';

export interface EmployeeFormProps {
  /** Present -> edit mode (PUT); absent -> create mode (POST). Proves both form->API paths
   * described in phase2.md 29. */
  editing?: Employee;
  onDone: () => void;
  onCancel: () => void;
}

/**
 * The Phase 2 UIPlatform.Forms consumer: metadata (employeeSchemaFields) -> FormService.buildForm
 * -> <Form> (real field registry + Zod validation from ui-platform-forms, not a hand-rolled
 * form). Submit goes through the real API client (employeeApi.ts) -> EmployeesController ->
 * CrudEngine -> SQL Server.
 */
export function EmployeeForm({ editing, onDone, onCancel }: EmployeeFormProps) {
  const config = useMemo(() => FormService.buildForm(employeeSchemaFields, { entityName: 'Employee' }), []);
  const createEmployee = useCreateEmployee();
  const updateEmployee = useUpdateEmployee(editing?.id ?? '');

  const mutation = editing ? updateEmployee : createEmployee;

  const defaultValues: Record<string, unknown> = editing
    ? {
        employeeCode: editing.employeeCode,
        name: editing.name,
        // Field-masked (Phase 1): null when the API omitted Email for this caller. Falls back to
        // '' rather than leaving the input undefined — this form is only ever reachable by a role
        // holding employee.update (PermissionGuard on the list page), which today always implies
        // Email access too, but the fallback keeps the form itself from breaking if that ever changes.
        email: editing.email ?? '',
        department: editing.department ?? '',
        isActive: editing.isActive,
      }
    : { isActive: true };

  const handleSubmit = async (values: Record<string, unknown>) => {
    await mutation.mutateAsync(values as unknown as EmployeeFormValues);
    onDone();
  };

  return (
    <div className="employee-form">
      <h2>{editing ? `Edit Employee — ${editing.employeeCode}` : 'New Employee'}</h2>
      {/* Real ui-platform-forms field registry + Zod validation (via <Field>, registry-backed
          and RHF-connected) drive every input below — this is not a hand-rolled form. */}
      <Form config={config} defaultValues={defaultValues} onSubmit={handleSubmit}>
        {config.fields.map((f) => (
          <Field key={f.name} name={f.name} />
        ))}
        <div className="form-actions">
          <button type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? 'Saving…' : 'Save'}
          </button>
          <button type="button" onClick={onCancel} className="secondary">
            Cancel
          </button>
        </div>
      </Form>
      {mutation.isError && (
        <p className="form-error">
          {isApiError(mutation.error) ? mutation.error.message : 'Request failed.'}
        </p>
      )}
    </div>
  );
}
