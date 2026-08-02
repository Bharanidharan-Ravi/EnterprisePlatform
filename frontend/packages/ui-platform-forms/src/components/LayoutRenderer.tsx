import type { LayoutNode } from '../types';
import { Section } from './Section';
import { Group } from './Group';
import { Tabs } from './Tabs';
import { Field } from './Field';

export function LayoutRenderer({ nodes }: { nodes: LayoutNode[] }) {
  return (
    <>
      {nodes.map((node, i) => {
        switch (node.kind) {
          case 'section':
            return <Section key={node.key} title={node.title} columns={node.columns}><LayoutRenderer nodes={node.children} /></Section>;
          case 'group':
            return <Group key={node.key} title={node.title} columns={node.columns}><LayoutRenderer nodes={node.children} /></Group>;
          case 'tabs':
            return <Tabs key={node.key} tabs={node.tabs} render={(children) => <LayoutRenderer nodes={children} />} />;
          case 'row':
            return <div key={i} className="flex flex-row gap-4">{node.children.map((c, j) => <LayoutRenderer key={j} nodes={[c]} />)}</div>;
          case 'field':
            return <Field key={node.name} name={node.name} />;
          default:
            return null;
        }
      })}
    </>
  );
}
