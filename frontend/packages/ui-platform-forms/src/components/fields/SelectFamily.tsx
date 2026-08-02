import type { FieldComponentProps } from '../../types';
import { useLookup } from '../../hooks/useLookup';

export function SelectField({ field, disabled, readOnly, inputProps }: FieldComponentProps) {
  const lookup = useLookup(field.lookupKey);
  const options = field.options ?? lookup.data ?? [];

  return (
    <select
      id={field.name}
      disabled={disabled || readOnly}
      className="border rounded px-3 py-2"
      name={inputProps.name}
      value={(inputProps.value as string) ?? ''}
      onChange={(e) => inputProps.onChange(e.target.value)}
      onBlur={inputProps.onBlur}
      ref={inputProps.ref as React.Ref<HTMLSelectElement>}
    >
      <option value="" disabled hidden>{field.placeholder ?? 'Select…'}</option>
      {options.map((opt) => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
    </select>
  );
}

export function MultiSelectField({ field, disabled, readOnly, inputProps }: FieldComponentProps) {
  const lookup = useLookup(field.lookupKey);
  const options = field.options ?? lookup.data ?? [];
  const selected = new Set((inputProps.value as string[]) ?? []);

  const toggle = (value: string) => {
    const next = new Set(selected);
    next.has(value) ? next.delete(value) : next.add(value);
    inputProps.onChange([...next]);
  };

  return (
    <div className="flex flex-col gap-1 border rounded p-2">
      {options.map((opt) => (
        <label key={opt.value} className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            disabled={disabled || readOnly}
            checked={selected.has(opt.value)}
            onChange={() => toggle(opt.value)}
          />
          {opt.label}
        </label>
      ))}
    </div>
  );
}
