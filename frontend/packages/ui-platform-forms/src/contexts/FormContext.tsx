import { createContext, useContext, useMemo, useRef, type ReactNode } from 'react';
import { FormProvider, useForm, type UseFormReturn } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import type { FormConfig, FormEvent, FormEventHandler } from '../types';
import { ValidationRegistry } from '../registry/ValidationRegistry';

interface FormContextValue {
  config: FormConfig;
  emit: (event: FormEvent) => void;
  subscribe: (handler: FormEventHandler) => () => void;
}

const NucleusFormContext = createContext<FormContextValue | undefined>(undefined);

export interface FormProviderProps {
  config: FormConfig;
  defaultValues?: Record<string, unknown>;
  onSubmit: (values: Record<string, unknown>) => void | Promise<void>;
  onCancel?: () => void;
  children: ReactNode;
}

/** Root form context: wires RHF + Zod (via ValidationRegistry) + a lightweight field-event bus. */
export function NucleusFormProvider({ config, defaultValues, onSubmit, children }: FormProviderProps) {
  const schema = useMemo(() => ValidationRegistry.buildFormSchema(config.fields), [config.fields]);
  const resolved = useMemo(() => {
    const values: Record<string, unknown> = {};
    for (const f of config.fields) values[f.name] = defaultValues?.[f.name] ?? f.defaultValue;
    return values;
  }, [config.fields, defaultValues]);

  const methods = useForm({
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    resolver: zodResolver(schema as any),
    defaultValues: resolved,
    mode: 'onBlur',
  });

  const handlers = useRef(new Set<FormEventHandler>());
  const emit = (event: FormEvent) => handlers.current.forEach((h) => h(event));
  const subscribe = (handler: FormEventHandler) => {
    handlers.current.add(handler);
    return () => handlers.current.delete(handler);
  };

  const value = useMemo<FormContextValue>(() => ({ config, emit, subscribe }), [config]);

  return (
    <NucleusFormContext.Provider value={value}>
      <FormProvider {...methods}>
        <form onSubmit={methods.handleSubmit((values) => { emit({ type: 'submit' }); return onSubmit(values); })}>
          {children}
        </form>
      </FormProvider>
    </NucleusFormContext.Provider>
  );
}

export function useNucleusFormContext(): FormContextValue {
  const ctx = useContext(NucleusFormContext);
  if (!ctx) throw new Error('useNucleusFormContext must be used within a <Form/>');
  return ctx;
}

export type { UseFormReturn };
