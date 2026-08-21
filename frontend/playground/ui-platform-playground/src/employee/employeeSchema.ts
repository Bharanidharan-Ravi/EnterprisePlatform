import type { SchemaFieldDefinition } from '@nucleus/uiplatform-forms';

/**
 * Hand-mirrored from backend/playground/APIPlatform.Playground/Metadata/
 * EmployeeEntityDefinitionProvider.cs's editable fields (Id/CreatedOn/ModifiedOn are excluded —
 * same as the backend's UiHint.Visible = false on those). "Same conceptual metadata" per
 * phase2.md 28 — not fetched, since no metadata HTTP endpoint exists in this phase.
 */
export const employeeSchemaFields: SchemaFieldDefinition[] = [
  {
    name: 'employeeCode',
    dataType: 'String',
    isNullable: false,
    validation: { required: true, maxLength: 20 },
    uiHint: { inputType: 'Text', displayLabel: 'Employee Code', columnWidth: 6 },
  },
  {
    name: 'name',
    dataType: 'String',
    isNullable: false,
    validation: { required: true, maxLength: 200 },
    uiHint: { inputType: 'Text', displayLabel: 'Name', columnWidth: 6 },
  },
  {
    name: 'email',
    dataType: 'String',
    isNullable: false,
    validation: { required: true, maxLength: 256 },
    uiHint: { inputType: 'Text', displayLabel: 'Email', columnWidth: 6 },
  },
  {
    name: 'department',
    dataType: 'String',
    isNullable: true,
    uiHint: { inputType: 'Text', displayLabel: 'Department', columnWidth: 6 },
  },
  {
    name: 'isActive',
    dataType: 'Boolean',
    isNullable: false,
    defaultValue: 'true',
    uiHint: { inputType: 'Checkbox', displayLabel: 'Active', columnWidth: 12 },
  },
];
