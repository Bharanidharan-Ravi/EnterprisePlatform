import { NucleusFormProvider, type FormProviderProps } from '../contexts/FormContext';
import { LayoutRenderer } from './LayoutRenderer';

export type FormProps = FormProviderProps;

/** Root form component: metadata (FormConfig) in, rendered + validated form out. */
export function Form({ config, defaultValues, onSubmit, onCancel, children }: FormProps) {
  return (
    <NucleusFormProvider config={config} defaultValues={defaultValues} onSubmit={onSubmit} onCancel={onCancel}>
      {children ?? <LayoutRenderer nodes={config.layout} />}
    </NucleusFormProvider>
  );
}
