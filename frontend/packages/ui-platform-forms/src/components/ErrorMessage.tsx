import { useFormContext as useRHFContext } from 'react-hook-form';

export function ErrorMessage({ name }: { name: string }) {
  const { formState } = useRHFContext();
  const message = formState.errors[name]?.message as string | undefined;
  if (!message) return null;
  return <p className="text-xs text-red-600">{message}</p>;
}
