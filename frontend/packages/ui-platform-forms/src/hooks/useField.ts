import { useController, useFormContext as useRHFContext, useWatch } from 'react-hook-form';
import { useNucleusFormContext } from '../contexts/FormContext';
import { evaluateCondition } from '../utils/conditions';
import type { FieldConfig } from '../types';

export interface UseFieldResult {
  field: FieldConfig;
  visible: boolean;
  disabled: boolean;
  readOnly: boolean;
  controller: ReturnType<typeof useController>;
}

/** Resolves a single field's config, live visibility/enable state, and RHF controller binding. */
export function useField(name: string): UseFieldResult {
  const { control } = useRHFContext();
  const { config, emit } = useNucleusFormContext();
  const field = config.fields.find((f) => f.name === name);
  if (!field) throw new Error(`useField: no FieldConfig registered for "${name}"`);

  const values = useWatch({ control }) as Record<string, unknown>;
  const visible = (field.visible ?? true) && evaluateCondition(field.visibilityRule, values);
  const enabled = evaluateCondition(field.enableRule, values);
  const disabled = Boolean(field.disabled) || !enabled;

  const controller = useController({
    name,
    control,
    rules: undefined,
  });

  const originalOnChange = controller.field.onChange;
  controller.field.onChange = (value: unknown) => {
    originalOnChange(value);
    emit({ type: 'change', fieldName: name, value });
  };

  return { field, visible, disabled, readOnly: Boolean(field.readOnly), controller };
}
