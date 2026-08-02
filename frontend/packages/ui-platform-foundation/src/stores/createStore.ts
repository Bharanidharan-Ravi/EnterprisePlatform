import { create, type StateCreator } from 'zustand';

/**
 * Thin wrapper around zustand's `create` so every Nucleus store is constructed the same way.
 * Foundation defines no concrete stores itself — feature packages create their own via this factory.
 */
export function createStore<T>(initializer: StateCreator<T>) {
  return create<T>()(initializer);
}
