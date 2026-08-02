import type { FieldConfig } from '../types';

export function Label({ field }: { field: FieldConfig }) {
  return (
    <label htmlFor={field.name} className="text-sm font-medium">
      {field.label}
      {field.validation?.required && <span className="text-red-600 ml-0.5">*</span>}
    </label>
  );
}
