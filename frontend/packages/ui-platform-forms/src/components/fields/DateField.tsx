import type { FieldComponentProps } from '../../types';

const htmlType: Record<string, string> = { date: 'date', time: 'time', datetime: 'datetime-local' };

export function DateField({ field, disabled, readOnly, inputProps }: FieldComponentProps) {
  return (
    <input
      id={field.name}
      type={htmlType[field.type] ?? 'date'}
      disabled={disabled}
      readOnly={readOnly}
      className="border rounded px-3 py-2"
      name={inputProps.name}
      value={(inputProps.value as string) ?? ''}
      onChange={(e) => inputProps.onChange(e.target.value)}
      onBlur={inputProps.onBlur}
      ref={inputProps.ref as React.Ref<HTMLInputElement>}
    />
  );
}
