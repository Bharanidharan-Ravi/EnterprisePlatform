import { FieldRegistry } from './FieldRegistry';
import { TextField, PasswordField, TextareaField } from '../components/fields/TextFamily';
import { NumberField, CheckboxField, SwitchField, RadioField } from '../components/fields/ChoiceFamily';
import { SelectField, MultiSelectField } from '../components/fields/SelectFamily';
import { DateField } from '../components/fields/DateField';
import { FileField, HiddenField } from '../components/fields/FileFamily';
import { LookupField } from '../components/fields/LookupField';

/** Called once at package import — registers a default component for every built-in FieldType.
 * Consuming apps may override any entry via FieldRegistry.register(type, MyComponent). */
export function registerBuiltInFields(): void {
  FieldRegistry.register('text', TextField);
  FieldRegistry.register('email', TextField);
  FieldRegistry.register('phone', TextField);
  FieldRegistry.register('url', TextField);
  FieldRegistry.register('textarea', TextareaField);
  FieldRegistry.register('richtext', TextareaField); // default stand-in; swap via FieldRegistry.register for a real rich-text editor
  FieldRegistry.register('password', PasswordField);

  FieldRegistry.register('number', NumberField);
  FieldRegistry.register('decimal', NumberField);
  FieldRegistry.register('currency', NumberField);
  FieldRegistry.register('percentage', NumberField);

  FieldRegistry.register('checkbox', CheckboxField);
  FieldRegistry.register('boolean', CheckboxField);
  FieldRegistry.register('switch', SwitchField);
  FieldRegistry.register('radio', RadioField);

  FieldRegistry.register('dropdown', SelectField);
  FieldRegistry.register('multiselect', MultiSelectField);

  FieldRegistry.register('date', DateField);
  FieldRegistry.register('time', DateField);
  FieldRegistry.register('datetime', DateField);

  FieldRegistry.register('file', FileField);
  FieldRegistry.register('image', FileField); // default stand-in; swap via FieldRegistry.register for an image preview/cropper
  FieldRegistry.register('hidden', HiddenField);

  FieldRegistry.register('lookup', LookupField);
  FieldRegistry.register('autocomplete', LookupField);
}
