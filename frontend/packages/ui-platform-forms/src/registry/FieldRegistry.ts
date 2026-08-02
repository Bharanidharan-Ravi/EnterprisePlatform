import type { ComponentType } from 'react';
import type { FieldComponentProps, FieldType } from '../types';

/**
 * Single source of truth: FieldType (or a custom component key) -> React component.
 * Built-in types are seeded by components/index (registerBuiltInFields) at package import time.
 * Consuming apps override a built-in via register(type, Component), or add new keys for
 * type: 'custom' fields (field.component names the key).
 */
class FieldRegistryImpl {
  private readonly components = new Map<string, ComponentType<FieldComponentProps>>();

  register(key: FieldType | string, component: ComponentType<FieldComponentProps>): void {
    this.components.set(key, component);
  }

  get(key: FieldType | string): ComponentType<FieldComponentProps> | undefined {
    return this.components.get(key);
  }

  has(key: FieldType | string): boolean {
    return this.components.has(key);
  }
}

export const FieldRegistry = new FieldRegistryImpl();
