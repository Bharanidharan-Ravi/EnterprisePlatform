import type { FieldComponentProps } from '../../types';

export function TextField({ field, disabled, readOnly, inputProps }: FieldComponentProps) {
  return (
    <input
      id={field.name}
      type="text"
      disabled={disabled}
      readOnly={readOnly}
      placeholder={field.placeholder}
      className="border rounded px-3 py-2"
      name={inputProps.name}
      value={(inputProps.value as string) ?? ''}
      onChange={(e) => inputProps.onChange(e.target.value)}
      onBlur={inputProps.onBlur}
      ref={inputProps.ref as React.Ref<HTMLInputElement>}
    />
  );
}

export function PasswordField({ field, disabled, readOnly, inputProps }: FieldComponentProps) {
  return (
    <input
      id={field.name}
      type="password"
      disabled={disabled}
      readOnly={readOnly}
      placeholder={field.placeholder}
      autoComplete="new-password"
      className="border rounded px-3 py-2"
      name={inputProps.name}
      value={(inputProps.value as string) ?? ''}
      onChange={(e) => inputProps.onChange(e.target.value)}
      onBlur={inputProps.onBlur}
      ref={inputProps.ref as React.Ref<HTMLInputElement>}
    />
  );
}

export function TextareaField({ field, disabled, readOnly, inputProps }: FieldComponentProps) {
  return (
    <textarea
      id={field.name}
      disabled={disabled}
      readOnly={readOnly}
      placeholder={field.placeholder}
      rows={4}
      className="border rounded px-3 py-2"
      name={inputProps.name}
      value={(inputProps.value as string) ?? ''}
      onChange={(e) => inputProps.onChange(e.target.value)}
      onBlur={inputProps.onBlur}
      ref={inputProps.ref as React.Ref<HTMLTextAreaElement>}
    />
  );
}
