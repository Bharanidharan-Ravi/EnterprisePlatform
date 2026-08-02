import type { ReactNode } from 'react';

export function Section({ title, columns = 1, children }: { title?: string; columns?: number; children: ReactNode }) {
  return (
    <section className="flex flex-col gap-3 mb-6">
      {title && <h3 className="text-base font-semibold">{title}</h3>}
      <div className="grid gap-4" style={{ gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))` }}>{children}</div>
    </section>
  );
}
