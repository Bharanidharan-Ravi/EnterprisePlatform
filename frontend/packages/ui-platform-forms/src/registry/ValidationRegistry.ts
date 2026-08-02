import { z, type ZodTypeAny } from 'zod';
import type { FieldConfig } from '../types';

export type CustomValidator = (value: unknown, values: Record<string, unknown>) => string | undefined;
export type AsyncValidator = (value: unknown, values: Record<string, unknown>) => Promise<string | undefined>;
export type CrossFieldRule = (values: Record<string, unknown>) => Record<string, string> | undefined;

/**
 * Assembles a Zod schema from FieldConfig[]. Built-in constraints (required/length/range/regex/
 * email) map directly; custom/async/cross-field rules are extension points registered by name —
 * ValidationRegistry never hardcodes business rules.
 */
class ValidationRegistryImpl {
  private readonly customValidators = new Map<string, CustomValidator>();
  private readonly asyncValidators = new Map<string, AsyncValidator>();
  private readonly crossFieldRules = new Map<string, CrossFieldRule>();

  registerCustomValidator(name: string, fn: CustomValidator): void {
    this.customValidators.set(name, fn);
  }

  registerAsyncValidator(name: string, fn: AsyncValidator): void {
    this.asyncValidators.set(name, fn);
  }

  registerCrossFieldRule(name: string, fn: CrossFieldRule): void {
    this.crossFieldRules.set(name, fn);
  }

  /** Builds the base (non-cross-field) Zod schema for a single field. */
  buildFieldSchema(field: FieldConfig): ZodTypeAny {
    const v = field.validation;
    let schema: ZodTypeAny;

    switch (field.type) {
      case 'number':
      case 'decimal':
      case 'currency':
      case 'percentage': {
        let num = z.number();
        if (v?.minValue !== undefined) num = num.min(v.minValue);
        if (v?.maxValue !== undefined) num = num.max(v.maxValue);
        schema = num;
        break;
      }
      case 'checkbox':
      case 'switch':
      case 'boolean':
        schema = z.boolean();
        break;
      case 'multiselect':
        schema = z.array(z.string());
        break;
      case 'date':
      case 'time':
      case 'datetime':
        schema = z.union([z.string(), z.date()]);
        break;
      default: {
        let str = z.string();
        if (v?.minLength !== undefined) str = str.min(v.minLength);
        if (v?.maxLength !== undefined) str = str.max(v.maxLength);
        if (v?.regexPattern) str = str.regex(new RegExp(v.regexPattern));
        if (v?.email || field.type === 'email') str = str.email();
        if (v?.url || field.type === 'url') str = str.url();
        schema = str;
        break;
      }
    }

    if (v?.customValidatorName) {
      const validator = this.customValidators.get(v.customValidatorName);
      if (validator) {
        schema = schema.superRefine((val, ctx) => {
          const message = validator(val, {});
          if (message) ctx.addIssue({ code: z.ZodIssueCode.custom, message });
        });
      }
    }

    if (v?.asyncValidatorName) {
      const validator = this.asyncValidators.get(v.asyncValidatorName);
      if (validator) {
        schema = schema.superRefine(async (val, ctx) => {
          const message = await validator(val, {});
          if (message) ctx.addIssue({ code: z.ZodIssueCode.custom, message });
        });
      }
    }

    const required = v?.required ?? false;
    return required ? schema : schema.optional().nullable();
  }

  /** Builds the full form schema: per-field schemas plus registered cross-field rules. */
  buildFormSchema(fields: FieldConfig[]): ZodTypeAny {
    const shape: Record<string, ZodTypeAny> = {};
    for (const field of fields) shape[field.name] = this.buildFieldSchema(field);

    const crossFieldNames = [...new Set(fields.map((f) => f.validation?.crossFieldRuleName).filter(Boolean))] as string[];

    let objectSchema: ZodTypeAny = z.object(shape);
    if (crossFieldNames.length > 0) {
      objectSchema = objectSchema.superRefine((values, ctx) => {
        for (const name of crossFieldNames) {
          const rule = this.crossFieldRules.get(name);
          const result = rule?.(values as Record<string, unknown>);
          if (!result) continue;
          for (const [fieldName, message] of Object.entries(result)) {
            ctx.addIssue({ code: z.ZodIssueCode.custom, message, path: [fieldName] });
          }
        }
      });
    }

    return objectSchema;
  }
}

export const ValidationRegistry = new ValidationRegistryImpl();
