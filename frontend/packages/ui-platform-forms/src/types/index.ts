/** UIPlatform.Forms — enterprise metadata-driven form engine. No CRUD/Auth/Workflow/business knowledge. */

/** Superset of Nucleus.SharedSchema.Enums.UiInputType — the renderable field types Forms supports. */
export type FieldType =
  | 'text' | 'textarea' | 'number' | 'decimal' | 'currency' | 'percentage'
  | 'date' | 'time' | 'datetime'
  | 'boolean' | 'checkbox' | 'switch' | 'radio'
  | 'dropdown' | 'multiselect' | 'autocomplete' | 'lookup'
  | 'password' | 'email' | 'phone' | 'url'
  | 'file' | 'image' | 'richtext' | 'hidden' | 'custom';

export interface FieldOption {
  value: string;
  label: string;
  meta?: Record<string, unknown>;
}

/** Serializable condition — never raw JS/eval, config stays data not code. */
export type ConditionalExpression =
  | { field: string; operator: 'eq' | 'neq' | 'in' | 'notIn' | 'truthy' | 'falsy' | 'gt' | 'lt' | 'gte' | 'lte'; value?: unknown }
  | { all: ConditionalExpression[] }
  | { any: ConditionalExpression[] };

/** Forms-internal field validation input — superset of SharedSchema's ValidationRuleDefinition. */
export interface FieldValidation {
  required?: boolean;
  minLength?: number;
  maxLength?: number;
  minValue?: number;
  maxValue?: number;
  regexPattern?: string;
  email?: boolean;
  url?: boolean;
  /** Name of a validator registered via ValidationRegistry.registerCustomValidator. */
  customValidatorName?: string;
  /** Name of an async validator registered via ValidationRegistry.registerAsyncValidator. */
  asyncValidatorName?: string;
  /** Name of a cross-field rule registered via ValidationRegistry.registerCrossFieldRule. */
  crossFieldRuleName?: string;
}

/** A single resolved, render-ready field — the output of FormBuilder for one FieldDefinition. */
export interface FieldConfig {
  name: string;
  type: FieldType;
  label: string;
  readOnly?: boolean;
  disabled?: boolean;
  defaultValue?: unknown;
  columnWidth?: number;
  visible?: boolean;
  visibilityRule?: ConditionalExpression;
  enableRule?: ConditionalExpression;
  validation?: FieldValidation;
  options?: FieldOption[];
  /** Key registered in LookupService, for 'lookup'/'autocomplete' types. */
  lookupKey?: string;
  /** Field names this field's visibility/options/value depend on — drives re-evaluation & lookup refetch. */
  dependsOn?: string[];
  /** For type: 'custom' — the component key registered in FieldRegistry. */
  component?: string;
  placeholder?: string;
  helpText?: string;
  meta?: Record<string, unknown>;
}

export type LayoutNode =
  | { kind: 'section'; key: string; title?: string; columns?: number; children: LayoutNode[] }
  | { kind: 'group'; key: string; title?: string; columns?: number; children: LayoutNode[] }
  | { kind: 'tabs'; key: string; tabs: { key: string; label: string; children: LayoutNode[] }[] }
  | { kind: 'row'; children: LayoutNode[] }
  | { kind: 'field'; name: string };

export interface FormConfig {
  entityName?: string;
  schemaVersion?: number;
  fields: FieldConfig[];
  layout: LayoutNode[];
}

export interface FormState {
  isDirty: boolean;
  dirtyFields: Record<string, boolean>;
  isSubmitting: boolean;
  isValid: boolean;
  values: Record<string, unknown>;
}

export type FormEventType = 'change' | 'blur' | 'submit' | 'reset' | 'cancel';
export interface FormEvent {
  type: FormEventType;
  fieldName?: string;
  value?: unknown;
}
export type FormEventHandler = (event: FormEvent) => void;

export type LookupOption = FieldOption;
export type LookupResolver = (params?: Record<string, unknown>) => Promise<LookupOption[]>;

/** Props every field component (built-in or custom) receives from <Field/>. */
export interface FieldComponentProps {
  field: FieldConfig;
  disabled: boolean;
  readOnly: boolean;
  /** RHF's controller field render props (value/onChange/onBlur/name/ref) — bind directly to the input. */
  inputProps: {
    name: string;
    value: unknown;
    onChange: (value: unknown) => void;
    onBlur: () => void;
    ref: (instance: unknown) => void;
  };
}
