import { useFormContext as useRHFContext, useFormState } from 'react-hook-form';
import { useNucleusFormContext } from '../contexts/FormContext';
import type { FormState } from '../types';

export interface UseFormContextResult {
  state: FormState;
  reset: () => void;
  cancel: () => void;
  config: ReturnType<typeof useNucleusFormContext>['config'];
}

/** Combined form state + actions. Must be used within <Form/>. */
export function useFormContext(): UseFormContextResult {
  const rhf = useRHFContext();
  const { config, emit } = useNucleusFormContext();
  const { isDirty, dirtyFields, isSubmitting, isValid } = useFormState({ control: rhf.control });

  return {
    state: {
      isDirty,
      dirtyFields: Object.fromEntries(Object.keys(dirtyFields).map((k) => [k, true])),
      isSubmitting,
      isValid,
      values: rhf.getValues(),
    },
    reset: () => { rhf.reset(); emit({ type: 'reset' }); },
    cancel: () => emit({ type: 'cancel' }),
    config,
  };
}
