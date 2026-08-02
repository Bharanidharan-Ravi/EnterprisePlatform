import type { FieldConfig, FormConfig, LayoutNode } from '../types';
import { resolveFieldType, resolveOptions, resolveValidation, type SchemaFieldDefinition } from '../types/schemaAdapter';
import { LayoutBuilder } from './LayoutBuilder';

export interface FormBuilderOptions {
  entityName?: string;
  schemaVersion?: number;
  layout?: LayoutNode[];
  fieldOverrides?: Record<string, Partial<FieldConfig>>;
  visibilityRules?: Record<string, FieldConfig['visibilityRule']>;
  enableRules?: Record<string, FieldConfig['enableRule']>;
  lookupKeys?: Record<string, string>;
  /** Optional extension point: consuming app wires RBAC (e.g. UIPlatform.Auth) here — Forms
   * itself never evaluates permissions. Return undefined to leave a flag unchanged. */
  resolveFieldAccess?: (field: SchemaFieldDefinition) => { readOnly?: boolean; hidden?: boolean } | undefined;
}

export const FormBuilder = {
  buildFormConfig(schemaFields: SchemaFieldDefinition[], options: FormBuilderOptions = {}): FormConfig {
    const fields: FieldConfig[] = schemaFields.map((f) => {
      const access = options.resolveFieldAccess?.(f);
      const base: FieldConfig = {
        name: f.name,
        type: resolveFieldType(f),
        label: f.uiHint?.displayLabel ?? f.name,
        readOnly: access?.readOnly ?? false,
        visible: access?.hidden ? false : (f.uiHint?.visible ?? true),
        columnWidth: f.uiHint?.columnWidth ?? undefined,
        defaultValue: f.defaultValue ?? undefined,
        options: resolveOptions(f),
        validation: resolveValidation(f),
        lookupKey: options.lookupKeys?.[f.name],
        visibilityRule: options.visibilityRules?.[f.name],
        enableRule: options.enableRules?.[f.name],
      };
      return { ...base, ...options.fieldOverrides?.[f.name] };
    });

    return {
      entityName: options.entityName,
      schemaVersion: options.schemaVersion,
      fields,
      layout: options.layout ?? LayoutBuilder.defaultLayout(fields),
    };
  },
};
