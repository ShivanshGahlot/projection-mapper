// Click-to-rename for a text element: swaps it for an input until Enter/blur/Esc.

import { h } from '../dom';

export function startRename(target: HTMLElement, current: string, commit: (name: string) => void): void {
  const input = h('input', 'rename-input');
  input.type = 'text';
  input.value = current;
  input.spellcheck = false;
  let done = false;
  const finish = (save: boolean) => {
    if (done) return;
    done = true;
    const v = input.value.trim();
    input.replaceWith(target);
    if (save && v && v !== current) commit(v);
  };
  input.addEventListener('keydown', (e) => {
    e.stopPropagation();
    if (e.key === 'Enter') finish(true);
    if (e.key === 'Escape') finish(false);
  });
  input.addEventListener('blur', () => finish(true));
  input.addEventListener('pointerdown', (e) => e.stopPropagation());
  input.addEventListener('dblclick', (e) => e.stopPropagation());
  target.replaceWith(input);
  input.focus();
  input.select();
}
