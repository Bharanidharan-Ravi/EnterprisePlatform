import type { FieldOption, FieldType, FieldValidation } from './index';

/**
 * ASSUMPTION BOUNDARY.
 * Nucleus.SharedSchema.Enums.UiInputType (Text/Dropdown/Checkbox/Radio/Calendar/Autocomplete/
 * TextArea) is coarser than Forms' FieldType — it cannot express Password/Email/Phone/URL/
 * Currency/Percentage/Switch/MultiSelect/File/Image/RichText. resolveFieldType() below gives a
 * best-effort default from UiInputType + DataType; for anything finer, pass a `fieldOverrides`
 * entry to FormBuilder.buildFormConfig (e.g. `{ ssn: { type: 'password' } }`). This mapping is
 * intentionally isolated to this one file — do not duplicate the switch elsewhere.
 */

/** Minimal structural shape Forms needs from Nucleus.SharedSchema.Models.FieldDefinition. Kept
 * as a local interface (not a hard package dependency) so Forms stays consumable without a
 * mandatory SharedSchema install — pass any object matching this shape. */
export interface SchemaFieldDefinition {
  name: string;
  dataType: 'String' | 'Integer' | 'Decimal' | 'Boolean' | 'DateTime' | 'Date' | 'Guid' | 'Enum';
  isNullable?: boolean;
  enumValues?: string[] | null;
  defaultValue?: string | null;
  sourcedViaRelationshipName?: string | null;
  validation?: {
    required?: boolean;
    minLength?: number | null;
    maxLength?: number | null;
    minValue?: number | null;
    maxValue?: number | null;
    regexPattern?: string | null;
    crossFieldRuleName?: string | null;
  } | null;
  uiHint?: {
    inputType: 'Text' | 'Dropdown' | 'Checkbox' | 'Radio' | 'Calendar' | 'Autocomplete' | 'TextArea';
    displayLabel: string;
    columnWidth?: number | null;
    visible?: boolean;
    visibilityRuleName?: string | null;
  } | null;
}

export function resolveFieldType(field: SchemaFieldDefinition): FieldType {
  const hint = field.uiHint?.inputType;
  const isLookup = Boolean(field.sourcedViaRelationshipName);

  switch (hint) {
    case 'Dropdown':
      return isLookup ? 'lookup' : field.dataType === 'Enum' ? 'dropdown' : 'dropdown';
    case 'Checkbox':
      return 'checkbox';
    case 'Radio':
      return 'radio';
    case 'Calendar':
      return field.dataType === 'DateTime' ? 'datetime' : 'date';
    case 'Autocomplete':
      return isLookup ? 'lookup' : 'autocomplete';
    case 'TextArea':
      return 'textarea';
    case 'Text':
    case undefined:
    default:
      break;
  }

  switch (field.dataType) {
    case 'Boolean': return 'checkbox';
    case 'DateTime': return 'datetime';
    case 'Date': return 'date';
    case 'Integer': return 'number';
    case 'Decimal': return 'decimal';
    case 'Enum': return 'dropdown';
    case 'Guid': return isLookup ? 'lookup' : 'text';
    case 'String':
    default: return 'text';
  }
}

export function resolveOptions(field: SchemaFieldDefinition): FieldOption[] | undefined {
  return field.enumValues?.map((v) => ({ value: v, label: v }));
}

export function resolveValidation(field: SchemaFieldDefinition): FieldValidation | undefined {
  const v = field.validation;
  if (!v && field.isNullable !== false) return undefined;
  return {
    required: v?.required ?? field.isNullable === false,
    minLength: v?.minLength ?? undefined,
    maxLength: v?.maxLength ?? undefined,
    minValue: v?.minValue ?? undefined,
    maxValue: v?.maxValue ?? undefined,
    regexPattern: v?.regexPattern ?? undefined,
    crossFieldRuleName: v?.crossFieldRuleName ?? undefined,
  };
}
