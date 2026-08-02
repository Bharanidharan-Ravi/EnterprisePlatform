import { useState, type ReactNode } from 'react';
import type { LayoutNode } from '../types';

export interface TabsProps {
  tabs: { key: string; label: string; children: LayoutNode[] }[];
  render: (nodes: LayoutNode[]) => ReactNode;
}

/** Minimal, unstyled-beyond-basics tab switcher. `render` is supplied by LayoutRenderer to avoid a circular import. */
export function Tabs({ tabs, render }: TabsProps) {
  const [active, setActive] = useState(tabs[0]?.key);
  const activeTab = tabs.find((t) => t.key === active) ?? tabs[0];

  return (
    <div className="flex flex-col gap-4">
      <div className="flex gap-2 border-b">
        {tabs.map((t) => (
          <button
            key={t.key}
            type="button"
            onClick={() => setActive(t.key)}
            className={`px-3 py-2 text-sm ${t.key === active ? 'border-b-2 border-blue-600 font-medium' : 'text-gray-500'}`}
          >
            {t.label}
          </button>
        ))}
      </div>
      {activeTab && render(activeTab.children)}
    </div>
  );
}
