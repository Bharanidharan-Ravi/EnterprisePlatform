import type { ConditionalExpression } from '../types';

export function evaluateCondition(expr: ConditionalExpression | undefined, values: Record<string, unknown>): boolean {
  if (!expr) return true;
  if ('all' in expr) return expr.all.every((e) => evaluateCondition(e, values));
  if ('any' in expr) return expr.any.some((e) => evaluateCondition(e, values));

  const actual = values[expr.field];
  switch (expr.operator) {
    case 'eq': return actual === expr.value;
    case 'neq': return actual !== expr.value;
    case 'in': return Array.isArray(expr.value) && expr.value.includes(actual);
    case 'notIn': return Array.isArray(expr.value) && !expr.value.includes(actual);
    case 'truthy': return Boolean(actual);
    case 'falsy': return !actual;
    case 'gt': return Number(actual) > Number(expr.value);
    case 'lt': return Number(actual) < Number(expr.value);
    case 'gte': return Number(actual) >= Number(expr.value);
    case 'lte': return Number(actual) <= Number(expr.value);
    default: return true;
  }
}
