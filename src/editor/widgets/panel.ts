// The common frame of every panel: a header (title, shortcut badge, actions) and a body.

import { h } from '../dom';

export interface PanelShell {
  el: HTMLElement;
  head: HTMLElement;
  /** Right-aligned header area for buttons. */
  actions: HTMLElement;
  body: HTMLElement;
  badge: HTMLElement;
  setCount(n: number | null): void;
}

export function panelShell(title: string, key: string, className: string): PanelShell {
  const badge = h('span', 'badge', key);
  badge.title = `Press ${key} to show or hide this panel`;
  const count = h('span', 'badge count');
  count.hidden = true;
  const actions = h('div', 'panel-actions');
  const head = h('div', 'panel-head', h('span', 'panel-title', title), badge, count, actions);
  const body = h('div', 'panel-body');
  const el = h('section', `panel ${className}`, head, body);
  return {
    el,
    head,
    actions,
    body,
    badge,
    setCount(n) {
      count.hidden = n === null;
      if (n !== null) count.textContent = String(n);
    },
  };
}
