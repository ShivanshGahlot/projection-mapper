// Dropdown menus, context menus, popovers, modals and the file picker.

import { h } from '../dom';

export interface MenuItem {
  label?: string;
  shortcut?: string;
  danger?: boolean;
  disabled?: boolean;
  /** Shows a green dot (e.g. a visible panel). */
  dot?: boolean;
  separator?: boolean;
  run?(): void;
}

let closeCurrent: (() => void) | null = null;

/** Closes whatever menu or popover is open. */
export function closeOverlays(): void {
  closeCurrent?.();
}

function mount(el: HTMLElement, x: number, y: number, keepOpenOn?: HTMLElement, onClose?: () => void): () => void {
  closeCurrent?.();
  el.style.left = `${x}px`;
  el.style.top = `${y}px`;
  document.body.append(el);
  const r = el.getBoundingClientRect();
  if (r.right > window.innerWidth - 4) el.style.left = `${Math.max(4, window.innerWidth - r.width - 4)}px`;
  if (r.bottom > window.innerHeight - 4) el.style.top = `${Math.max(4, window.innerHeight - r.height - 4)}px`;
  const onDown = (e: PointerEvent) => {
    const t = e.target as Node;
    if (el.contains(t) || keepOpenOn?.contains(t)) return;
    close();
  };
  const onKey = (e: KeyboardEvent) => {
    if (e.key === 'Escape') close();
  };
  const close = () => {
    if (!el.isConnected) return;
    el.remove();
    onClose?.();
    document.removeEventListener('pointerdown', onDown, true);
    document.removeEventListener('keydown', onKey, true);
    if (closeCurrent === close) closeCurrent = null;
  };
  document.addEventListener('pointerdown', onDown, true);
  document.addEventListener('keydown', onKey, true);
  closeCurrent = close;
  return close;
}

function buildMenu(items: MenuItem[], close: () => void): HTMLElement {
  const menu = h('div', 'menu');
  for (const it of items) {
    if (it.separator) {
      menu.append(h('div', 'menu-sep'));
      continue;
    }
    const row = h('button', `menu-item${it.danger ? ' danger' : ''}`);
    row.type = 'button';
    row.disabled = !!it.disabled;
    if (it.dot !== undefined) row.append(h('span', `menu-dot${it.dot ? ' on' : ''}`));
    row.append(h('span', 'menu-label', it.label));
    if (it.shortcut) row.append(h('span', 'menu-shortcut', it.shortcut));
    row.addEventListener('click', () => {
      close();
      it.run?.();
    });
    menu.append(row);
  }
  return menu;
}

/** A dropdown that hangs below `anchor`. */
export function openMenu(anchor: HTMLElement, items: MenuItem[]): void {
  const r = anchor.getBoundingClientRect();
  let close = () => {};
  const menu = buildMenu(items, () => close());
  close = mount(menu, r.left, r.bottom + 4, anchor);
}

/** A menu at a screen position (right-click). */
export function openContextMenu(x: number, y: number, items: MenuItem[]): void {
  let close = () => {};
  const menu = buildMenu(items, () => close());
  close = mount(menu, x, y);
}

/** A floating panel under `anchor` that stays open until you click outside. */
export function openPopover(anchor: HTMLElement, content: HTMLElement, onClose?: () => void): () => void {
  const r = anchor.getBoundingClientRect();
  const pop = h('div', 'popover', content);
  return mount(pop, Math.max(4, r.right - 260), r.bottom + 4, anchor, onClose);
}

export function openModal(title: string, body: HTMLElement, width = 480): () => void {
  closeOverlays();
  const close = () => {
    backdrop.remove();
    document.removeEventListener('keydown', onKey, true);
  };
  const onKey = (e: KeyboardEvent) => {
    if (e.key === 'Escape') close();
  };
  const x = h('button', 'icon-btn', '✕');
  x.type = 'button';
  x.title = 'Close';
  x.addEventListener('click', close);
  const modal = h('div', 'modal', h('div', 'modal-head', h('span', undefined, title), x), body);
  modal.style.width = `${width}px`;
  const backdrop = h('div', 'backdrop', modal);
  backdrop.addEventListener('pointerdown', (e) => {
    if (e.target === backdrop) close();
  });
  document.addEventListener('keydown', onKey, true);
  document.body.append(backdrop);
  return close;
}

/** Simple yes/no question. */
export function confirmDialog(title: string, message: string, confirmLabel: string, danger = false): Promise<boolean> {
  return new Promise((resolve) => {
    const yes = h('button', danger ? 'btn danger' : 'btn primary', confirmLabel);
    const no = h('button', 'btn', 'Cancel');
    let close = () => {};
    let answered = false;
    const answer = (v: boolean) => {
      if (answered) return;
      answered = true;
      close();
      resolve(v);
    };
    yes.addEventListener('click', () => answer(true));
    no.addEventListener('click', () => answer(false));
    const body = h('div', 'modal-body', h('p', undefined, message), h('div', 'modal-actions', no, yes));
    close = openModal(title, body, 380);
    // Closing with Esc / backdrop counts as "no".
    new MutationObserver((_m, obs) => {
      if (!document.body.contains(body)) {
        obs.disconnect();
        answer(false);
      }
    }).observe(document.body, { childList: true });
    yes.focus();
  });
}

/** Opens the system file picker; resolves with the chosen file or null. */
export function pickFile(accept: string): Promise<File | null> {
  return new Promise((resolve) => {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = accept;
    input.addEventListener('change', () => resolve(input.files?.[0] ?? null));
    input.addEventListener('cancel', () => resolve(null));
    input.click();
  });
}
