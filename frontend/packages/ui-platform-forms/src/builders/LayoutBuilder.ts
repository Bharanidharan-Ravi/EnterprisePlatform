import type { FieldConfig, LayoutNode } from '../types';

/** Small factory helpers + a sensible default (single section, document order) when no layout is supplied. */
export const LayoutBuilder = {
  section(key: string, children: LayoutNode[], opts?: { title?: string; columns?: number }): LayoutNode {
    return { kind: 'section', key, children, title: opts?.title, columns: opts?.columns };
  },
  group(key: string, children: LayoutNode[], opts?: { title?: string; columns?: number }): LayoutNode {
    return { kind: 'group', key, children, title: opts?.title, columns: opts?.columns };
  },
  tabs(key: string, tabs: { key: string; label: string; children: LayoutNode[] }[]): LayoutNode {
    return { kind: 'tabs', key, tabs };
  },
  row(children: LayoutNode[]): LayoutNode {
    return { kind: 'row', children };
  },
  field(name: string): LayoutNode {
    return { kind: 'field', name };
  },

  /** Default layout: one unlabeled section containing every visible field in declaration order. */
  defaultLayout(fields: FieldConfig[]): LayoutNode[] {
    return [
      {
        kind: 'section',
        key: 'default',
        children: fields.map((f) => ({ kind: 'field' as const, name: f.name })),
      },
    ];
  },
};
