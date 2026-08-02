import { useEffect } from 'react';
import { useNucleusFormContext } from '../contexts/FormContext';
import type { FormEventHandler } from '../types';

/** Subscribes to form-level events (change/blur/submit/reset/cancel). Useful for dependent-field logic. */
export function useFormEvents(handler: FormEventHandler): void {
  const { subscribe } = useNucleusFormContext();
  useEffect(() => subscribe(handler), [subscribe, handler]);
}
