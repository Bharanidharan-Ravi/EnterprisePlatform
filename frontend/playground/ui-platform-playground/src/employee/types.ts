/**
 * Mirrors backend/playground/APIPlatform.Playground/Models/Employee.cs field-for-field. There is
 * no metadata-serving HTTP endpoint in this phase (out of scope per phase2.md), so this type and
 * employeeSchema.ts's SchemaFieldDefinition[] are both hand-mirrored from the backend's
 * EmployeeEntityDefinitionProvider rather than fetched — documented here, and in the Phase 2
 * report, as a manual mirror, not a live contract.
 */
export interface Employee {
  id: string;
  employeeCode: string;
  name: string;
  /** Field-masked (Phase 1): null when the caller lacks Email access — currently only
   * employee-admin holds it. Never re-derive visibility client-side; render whatever the API sent. */
  email: string | null;
  department: string | null;
  isActive: boolean;
  createdOn: string;
  modifiedOn: string | null;
}

/** Shape the Employee form actually edits — excludes Id/CreatedOn/ModifiedOn, which the platform
 * (GenericRepository's key handling + IEntityDefaultValueProvider) manages, never the form. */
export interface EmployeeFormValues {
  employeeCode: string;
  name: string;
  email: string;
  department?: string;
  isActive: boolean;
}
