// Types
export * from './types';
export type { SchemaFieldDefinition } from './types/schemaAdapter';
export { resolveFieldType } from './types/schemaAdapter';

// Registry
export { FieldRegistry } from './registry/FieldRegistry';
export { ValidationRegistry } from './registry/ValidationRegistry';
export { registerBuiltInFields } from './registry/registerBuiltInFields';

// Services
export { FormService } from './services/FormService';
export { LookupService } from './services/LookupService';

// Builders
export { FormBuilder, type FormBuilderOptions } from './builders/FormBuilder';
export { LayoutBuilder } from './builders/LayoutBuilder';

// Contexts
export { NucleusFormProvider, useNucleusFormContext } from './contexts/FormContext';

// Hooks
export { useFormContext } from './hooks/useFormContext';
export { useField } from './hooks/useField';
export { useLookup } from './hooks/useLookup';
export { useFormEvents } from './hooks/useFormEvents';

// Components
export { Form, type FormProps } from './components/Form';
export { Field } from './components/Field';
export { Section } from './components/Section';
export { Group } from './components/Group';
export { Tabs } from './components/Tabs';
export { Label } from './components/Label';
export { ErrorMessage } from './components/ErrorMessage';
export { LookupField } from './components/fields/LookupField';
export { DateField } from './components/fields/DateField';
export { TextField, PasswordField, TextareaField } from './components/fields/TextFamily';
export { NumberField, CheckboxField, SwitchField, RadioField } from './components/fields/ChoiceFamily';
export { SelectField, MultiSelectField } from './components/fields/SelectFamily';
export { FileField, HiddenField } from './components/fields/FileFamily';

// Utils
export { evaluateCondition } from './utils/conditions';

// Side effect: seed FieldRegistry with default components for every built-in FieldType.
import { registerBuiltInFields } from './registry/registerBuiltInFields';
registerBuiltInFields();
