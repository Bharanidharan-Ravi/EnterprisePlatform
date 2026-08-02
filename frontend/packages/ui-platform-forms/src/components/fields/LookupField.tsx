import { useState } from 'react';
import type { FieldComponentProps } from '../../types';
import { useLookup } from '../../hooks/useLookup';

export function LookupField({ field, disabled, readOnly, inputProps }: FieldComponentProps) {
  const [search, setSearch] = useState('');
  const [open, setOpen] = useState(false);
  const lookup = useLookup(field.lookupKey, { search });
  const selectedLabel = field.options?.find((o) => o.value === inputProps.value)?.label ?? (inputProps.value as string) ?? '';

  return (
    <div className="relative">
      <input
        id={field.name}
        type="text"
        disabled={disabled}
        readOnly={readOnly}
        placeholder={field.placeholder ?? 'Search…'}
        className="border rounded px-3 py-2 w-full"
        value={open ? search : selectedLabel}
        onFocus={() => setOpen(true)}
        onChange={(e) => setSearch(e.target.value)}
        onBlur={() => { setOpen(false); inputProps.onBlur(); }}
      />
      {open && (
        <ul className="absolute z-10 w-full border rounded bg-white shadow max-h-48 overflow-auto mt-1">
          {(lookup.data ?? []).map((opt) => (
            <li
              key={opt.value}
              className="px-3 py-2 text-sm hover:bg-gray-100 cursor-pointer"
              onMouseDown={() => { inputProps.onChange(opt.value); setOpen(false); }}
            >
              {opt.label}
            </li>
          ))}
          {lookup.isLoading && <li className="px-3 py-2 text-sm text-gray-400">Loading…</li>}
        </ul>
      )}
    </div>
  );
}
