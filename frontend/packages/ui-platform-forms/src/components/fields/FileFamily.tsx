import type { FieldComponentProps } from '../../types';

export function FileField({ field, disabled, readOnly, inputProps }: FieldComponentProps) {
  return (
    <input
      id={field.name}
      type="file"
      accept={field.type === 'image' ? 'image/*' : undefined}
      disabled={disabled || readOnly}
      className="text-sm"
      name={inputProps.name}
      onChange={(e) => inputProps.onChange(e.target.files?.[0] ?? null)}
      onBlur={inputProps.onBlur}
    />
  );
}

export function HiddenField({ inputProps }: FieldComponentProps) {
  return <input type="hidden" name={inputProps.name} value={(inputProps.value as string) ?? ''} ref={inputProps.ref as React.Ref<HTMLInputElement>} />;
}
