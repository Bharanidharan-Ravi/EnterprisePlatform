import type { FieldComponentProps } from '../../types';

export function NumberField({ field, disabled, readOnly, inputProps }: FieldComponentProps) {
  return (
    <input
      id={field.name}
      type="number"
      disabled={disabled}
      readOnly={readOnly}
      placeholder={field.placeholder}
      step={field.type === 'decimal' || field.type === 'currency' || field.type === 'percentage' ? 'any' : 1}
      className="border rounded px-3 py-2"
      name={inputProps.name}
      value={(inputProps.value as number) ?? ''}
      onChange={(e) => inputProps.onChange(e.target.value === '' ? undefined : Number(e.target.value))}
      onBlur={inputProps.onBlur}
      ref={inputProps.ref as React.Ref<HTMLInputElement>}
    />
  );
}

export function CheckboxField({ field, disabled, readOnly, inputProps }: FieldComponentProps) {
  return (
    <label className="flex items-center gap-2 text-sm">
      <input
        id={field.name}
        type="checkbox"
        disabled={disabled}
        readOnly={readOnly}
        name={inputProps.name}
        checked={Boolean(inputProps.value)}
        onChange={(e) => inputProps.onChange(e.target.checked)}
        onBlur={inputProps.onBlur}
        ref={inputProps.ref as React.Ref<HTMLInputElement>}
      />
      {field.label}
    </label>
  );
}

export function SwitchField({ field, disabled, readOnly, inputProps }: FieldComponentProps) {
  const checked = Boolean(inputProps.value);
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      disabled={disabled || readOnly}
      onClick={() => inputProps.onChange(!checked)}
      className={`w-10 h-6 rounded-full transition-colors ${checked ? 'bg-blue-600' : 'bg-gray-300'} relative`}
    >
      <span className={`absolute top-1 h-4 w-4 rounded-full bg-white transition-transform ${checked ? 'translate-x-5' : 'translate-x-1'}`} />
      <span className="sr-only">{field.label}</span>
    </button>
  );
}

export function RadioField({ field, disabled, readOnly, inputProps }: FieldComponentProps) {
  return (
    <div className="flex flex-col gap-2">
      {field.options?.map((opt) => (
        <label key={opt.value} className="flex items-center gap-2 text-sm">
          <input
            type="radio"
            name={inputProps.name}
            value={opt.value}
            disabled={disabled}
            readOnly={readOnly}
            checked={inputProps.value === opt.value}
            onChange={() => inputProps.onChange(opt.value)}
            onBlur={inputProps.onBlur}
          />
          {opt.label}
        </label>
      ))}
    </div>
  );
}
