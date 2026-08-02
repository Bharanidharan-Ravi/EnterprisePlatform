import type { SchemaFieldDefinition } from '../types/schemaAdapter';
import { FormBuilder, type FormBuilderOptions } from '../builders/FormBuilder';
import { ValidationRegistry } from '../registry/ValidationRegistry';
import type { FormConfig } from '../types';

/** Package entrypoint: SchemaFieldDefinition[] -> ready-to-render FormConfig + Zod schema. */
export const FormService = {
  buildForm(schemaFields: SchemaFieldDefinition[], options?: FormBuilderOptions): FormConfig {
    return FormBuilder.buildFormConfig(schemaFields, options);
  },
  buildSchema(config: FormConfig) {
    return ValidationRegistry.buildFormSchema(config.fields);
  },
};
