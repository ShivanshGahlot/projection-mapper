// Scrubbable number field: drag sideways to change the value (Shift = fine),
// click to type, Enter commits, Esc cancels.

import { clamp, h } from '../dom';
import { icon, type IconName } from '../icons';

/** Lets widgets end an undo "merge run" without knowing about the store. */
export const hooks = { endMerge: () => {} };

export interface NumberFieldOptions {
  label?: string;
  icon?: IconName;
  title?: string;
  get(): number;
  /** merge = true while scrubbing (one undo step per drag); false for a typed commit. */
  set(value: number, merge: boolean): void;
  min?: number;
  max?: number;
  step?: number;
  decimals?: number;
  unit?: string;
  /** Called after a scrub or typed commit finished. */
  onEnd?(): void;
  disabled?(): boolean;
}

export interface Field {
  el: HTMLElement;
  sync(): void;
}

export function formatNumber(v: number, decimals: number): string {
  return String(Number(v.toFixed(decimals)));
}

export function numberField(o: NumberFieldOptions): Field {
  const min = o.min ?? -Infinity;
  const max = o.max ?? Infinity;
  const step = o.step ?? 1;
  const decimals = o.decimals ?? Math.max(0, Math.min(4, Math.ceil(-Math.log10(step) - 1e-9)));
  const perPx = Number.isFinite(max - min) ? Math.max(step / 4, (max - min) / 240) : step;

  const lbl = h('span', 'nf-label');
  if (o.icon) lbl.append(icon(o.icon, 13));
  else if (o.label) lbl.textContent = o.label;
  const input = h('input', 'nf-input');
  input.type = 'text';
  input.inputMode = 'decimal';
  input.spellcheck = false;
  const unit = o.unit ? h('span', 'nf-unit', o.unit) : null;
  const el = h('div', 'nf', lbl, input, unit);
  if (o.title) el.title = o.title;

  const sync = () => {
    const off = o.disabled?.() ?? false;
    el.classList.toggle('disabled', off);
    input.disabled = off;
    if (document.activeElement !== input) input.value = formatNumber(o.get(), decimals);
  };

  let drag: { x: number; v: number; moved: boolean; id: number } | null = null;
  el.addEventListener('pointerdown', (e) => {
    if (e.button !== 0 || document.activeElement === input || input.disabled) return;
    e.preventDefault();
    drag = { x: e.clientX, v: o.get(), moved: false, id: e.pointerId };
    el.setPointerCapture(e.pointerId);
  });
  el.addEventListener('pointermove', (e) => {
    if (!drag || e.pointerId !== drag.id) return;
    const dx = e.clientX - drag.x;
    if (!drag.moved && Math.abs(dx) < 3) return;
    if (!drag.moved) {
      drag.moved = true;
      el.classList.add('scrubbing');
    }
    const speed = e.shiftKey ? 0.1 : 1;
    const v = clamp(drag.v + dx * perPx * speed, min, max);
    o.set(Number(v.toFixed(decimals + (e.shiftKey ? 1 : 0))), true);
    input.value = formatNumber(o.get(), decimals);
  });
  const finish = (e: PointerEvent) => {
    if (!drag || e.pointerId !== drag.id) return;
    const moved = drag.moved;
    drag = null;
    el.classList.remove('scrubbing');
    if (moved) {
      hooks.endMerge();
      o.onEnd?.();
      sync();
    } else {
      input.focus();
      input.select();
    }
  };
  el.addEventListener('pointerup', finish);
  el.addEventListener('pointercancel', finish);

  let cancelled = false;
  const commit = () => {
    if (cancelled) {
      cancelled = false;
      sync();
      return;
    }
    const v = parseFloat(input.value.replace(',', '.'));
    if (Number.isFinite(v) && v !== o.get()) {
      o.set(clamp(v, min, max), false);
      o.onEnd?.();
    }
    sync();
  };
  input.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') input.blur();
    else if (e.key === 'Escape') {
      cancelled = true;
      input.blur();
    } else if (e.key === 'ArrowUp' || e.key === 'ArrowDown') {
      e.preventDefault();
      const d = (e.key === 'ArrowUp' ? 1 : -1) * step * (e.shiftKey ? 10 : 1);
      o.set(clamp(Number((o.get() + d).toFixed(decimals)), min, max), false);
      o.onEnd?.();
      sync();
      input.select();
    }
  });
  input.addEventListener('blur', commit);
  sync();
  return { el, sync };
}
