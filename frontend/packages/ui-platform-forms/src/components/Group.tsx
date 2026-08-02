import type { ReactNode } from 'react';

export function Group({ title, columns = 1, children }: { title?: string; columns?: number; children: ReactNode }) {
  return (
    <fieldset className="flex flex-col gap-3 border rounded p-4">
      {title && <legend className="text-sm font-medium px-1">{title}</legend>}
      <div className="grid gap-4" style={{ gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))` }}>{children}</div>
    </fieldset>
  );
}
