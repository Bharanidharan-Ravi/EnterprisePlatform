import { useField } from '../hooks/useField';
import { FieldRegistry } from '../registry/FieldRegistry';
import { Label } from './Label';
import { ErrorMessage } from './ErrorMessage';

export function Field({ name }: { name: string }) {
  const { field, visible, disabled, readOnly, controller } = useField(name);
  if (!visible) return null;

  const key = field.type === 'custom' ? (field.component ?? 'custom') : field.type;
  const Component = FieldRegistry.get(key);
  if (!Component) throw new Error(`Field "${name}": no component registered for type/key "${key}"`);

  return (
    <div className="flex flex-col gap-1" style={field.columnWidth ? { gridColumn: `span ${field.columnWidth}` } : undefined}>
      {field.type !== 'hidden' && field.type !== 'checkbox' && field.type !== 'switch' && <Label field={field} />}
      <Component field={field} disabled={disabled} readOnly={readOnly} inputProps={controller.field} />
      {field.helpText && <p className="text-xs text-gray-500">{field.helpText}</p>}
      <ErrorMessage name={name} />
    </div>
  );
}
