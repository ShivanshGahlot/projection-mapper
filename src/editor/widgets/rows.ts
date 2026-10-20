// Labelled property rows (number, slider, select, toggle, colour, button group).
// Every row has sync(), which re-reads the value so panels can update in place
// instead of being rebuilt while the user drags.

import { h, normaliseHex } from '../dom';
import { icon, type IconName } from '../icons';
import { formatNumber, hooks, numberField, type Field, type NumberFieldOptions } from './numberField';

export interface Row {
  el: HTMLElement;
  sync(): void;
}

/** A group of rows that can be synced together. */
export class Section implements Row {
  el: HTMLElement;
  private rows: Row[] = [];
  constructor(className = 'section') {
    this.el = h('div', className);
  }
  add(...rows: Row[]): this {
    for (const r of rows) {
      this.rows.push(r);
      this.el.append(r.el);
    }
    return this;
  }
  /** Adds an element that has nothing to sync. */
  addEl(el: HTMLElement): this {
    this.el.append(el);
    return this;
  }
  sync(): void {
    for (const r of this.rows) r.sync();
  }
}

/** A section whose contents are rebuilt when `sig()` changes (e.g. a different fill type). */
export function dynamicSection(sig: () => string, build: () => Section): Row {
  const el = h('div', 'dynamic');
  let current = '';
  let inner: Section | null = null;
  const sync = () => {
    const s = sig();
    if (s !== current || !inner) {
      current = s;
      inner = build();
      el.replaceChildren(inner.el);
    }
    inner.sync();
  };
  sync();
  return { el, sync };
}

export function labelled(label: string, control: HTMLElement, title?: string, reset?: HTMLElement | null): HTMLElement {
  const lab = h('span', 'row-label', label);
  const r = h('div', 'row', lab, h('div', 'row-ctl', control), reset);
  if (title) r.title = title;
  return r;
}

export function resetButton(onClick: () => void, title = 'Reset to default'): HTMLButtonElement {
  const b = h('button', 'icon-btn reset');
  b.type = 'button';
  b.title = title;
  b.append(icon('reset', 12));
  b.addEventListener('click', onClick);
  return b;
}

export function sectionLabel(text: string, action?: HTMLElement): HTMLElement {
  return h('div', 'section-label', h('span', undefined, text), action);
}

/** Two number fields side by side, like "Opacity | Rotation". */
export function pair(a: Row, b: Row): Row {
  return {
    el: h('div', 'pair', a.el, b.el),
    sync: () => {
      a.sync();
      b.sync();
    },
  };
}

export function numberRow(o: NumberFieldOptions & { rowLabel?: string; tip?: string; reset?: number }): Row {
  const f = numberField(o);
  if (!o.rowLabel) return f;
  return { el: labelled(o.rowLabel, f.el, o.tip), sync: f.sync };
}

export interface SliderOptions {
  label: string;
  tip?: string;
  get(): number;
  set(v: number, merge: boolean): void;
  min: number;
  max: number;
  step: number;
  decimals?: number;
  unit?: string;
  /** Shows a per-row reset button when the value differs from this. */
  def?: number;
  /** Shows the label above the control instead of beside it. */
  stacked?: boolean;
}

export function sliderRow(o: SliderOptions): Row {
  const range = h('input', 'slider');
  range.type = 'range';
  range.min = String(o.min);
  range.max = String(o.max);
  range.step = String(o.step);
  const num = numberField({
    get: o.get,
    set: o.set,
    min: o.min,
    max: o.max,
    step: o.step,
    decimals: o.decimals,
    unit: o.unit,
    onEnd: () => sync(),
  });
  const reset = o.def !== undefined ? resetButton(() => { o.set(o.def!, false); sync(); }) : null;
  const ctl = h('div', 'slider-ctl', range, num.el);
  const sync = () => {
    const v = o.get();
    range.value = String(v);
    num.sync();
    if (reset) reset.style.visibility = Math.abs(v - o.def!) > 1e-9 ? 'visible' : 'hidden';
  };
  range.addEventListener('input', () => {
    o.set(range.valueAsNumber, true);
    num.sync();
    if (reset) reset.style.visibility = Math.abs(range.valueAsNumber - o.def!) > 1e-9 ? 'visible' : 'hidden';
  });
  range.addEventListener('change', () => {
    hooks.endMerge();
    sync();
  });
  const el = o.stacked !== false
    ? h('div', 'row stacked', h('div', 'row-head', h('span', 'row-label', o.label), reset), ctl)
    : h('div', 'row', h('span', 'row-label', o.label), h('div', 'row-ctl', ctl), reset);
  if (o.tip) el.title = o.tip;
  sync();
  return { el, sync };
}

export interface SelectOption {
  value: string;
  label: string;
  group?: string;
}

export function selectControl(options: SelectOption[], get: () => string, set: (v: string) => void): Row {
  const sel = h('select', 'select');
  const groups = new Map<string, HTMLElement>();
  for (const o of options) {
    const opt = h('option', undefined, o.label);
    opt.value = o.value;
    if (o.group) {
      let g = groups.get(o.group);
      if (!g) {
        g = h('optgroup');
        (g as HTMLOptGroupElement).label = o.group;
        groups.set(o.group, g);
        sel.append(g);
      }
      g.append(opt);
    } else sel.append(opt);
  }
  const sync = () => {
    if (sel.value !== get()) sel.value = get();
  };
  sel.addEventListener('change', () => set(sel.value));
  sync();
  return { el: sel, sync };
}

export function selectRow(o: { label: string; tip?: string; options: SelectOption[]; get(): string; set(v: string): void }): Row {
  const c = selectControl(o.options, o.get, o.set);
  return { el: labelled(o.label, c.el, o.tip), sync: c.sync };
}

export function toggleControl(get: () => boolean, set: (v: boolean) => void): Row {
  const box = h('input');
  box.type = 'checkbox';
  const el = h('label', 'switch', box, h('span', 'switch-track'));
  box.addEventListener('change', () => set(box.checked));
  const sync = () => {
    box.checked = get();
  };
  sync();
  return { el, sync };
}

export function toggleRow(o: { label: string; tip?: string; get(): boolean; set(v: boolean): void; reset?: HTMLElement | null }): Row {
  const c = toggleControl(o.get, o.set);
  return { el: labelled(o.label, c.el, o.tip, o.reset), sync: c.sync };
}

export function colorControl(get: () => string, set: (hex: string, merge: boolean) => void): Row {
  const picker = h('input');
  picker.type = 'color';
  const swatch = h('label', 'swatch', picker);
  const text = h('input', 'hex');
  text.type = 'text';
  text.spellcheck = false;
  text.maxLength = 9;
  const el = h('div', 'color-ctl', swatch, text);
  const sync = () => {
    const c = get();
    picker.value = c;
    swatch.style.background = c;
    if (document.activeElement !== text) text.value = c;
  };
  picker.addEventListener('input', () => {
    set(picker.value, true);
    sync();
  });
  picker.addEventListener('change', () => {
    hooks.endMerge();
    sync();
  });
  const commit = () => {
    const hex = normaliseHex(text.value);
    if (hex && hex !== get()) set(hex, false);
    sync();
  };
  text.addEventListener('blur', commit);
  text.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') text.blur();
    if (e.key === 'Escape') {
      text.value = get();
      text.blur();
    }
  });
  sync();
  return { el, sync };
}

export function colorRow(o: { label: string; tip?: string; get(): string; set(hex: string, merge: boolean): void; reset?: HTMLElement | null }): Row {
  const c = colorControl(o.get, o.set);
  return { el: labelled(o.label, c.el, o.tip, o.reset), sync: c.sync };
}

export interface SegOption<T extends string> {
  value: T;
  icon?: IconName;
  label?: string;
  title: string;
}

/** A row of mutually exclusive buttons. */
export function segmented<T extends string>(options: SegOption<T>[], get: () => T, set: (v: T) => void): Row {
  const btns = options.map((o) => {
    const b = h('button', 'seg-btn');
    b.type = 'button';
    b.title = o.title;
    if (o.icon) b.append(icon(o.icon, 14));
    if (o.label) b.append(o.label);
    b.addEventListener('click', () => set(o.value));
    return b;
  });
  const sync = () => options.forEach((o, i) => btns[i].classList.toggle('active', get() === o.value));
  sync();
  return { el: h('div', 'seg', ...btns), sync };
}

/** A single on/off icon button. */
export function toggleButton(name: IconName, title: string, get: () => boolean, set: (v: boolean) => void): Row {
  const b = h('button', 'seg-btn single');
  b.type = 'button';
  b.title = title;
  b.append(icon(name, 14));
  b.addEventListener('click', () => set(!get()));
  const sync = () => b.classList.toggle('active', get());
  sync();
  return { el: h('div', 'seg', b), sync };
}

/** A row of one-shot icon buttons (no selected state), e.g. alignment. */
export function actionButtons(items: { icon: IconName; title: string; run(): void }[]): HTMLElement {
  const btns = items.map((o) => {
    const b = h('button', 'seg-btn');
    b.type = 'button';
    b.title = o.title;
    b.append(icon(o.icon, 14));
    b.addEventListener('click', o.run);
    return b;
  });
  return h('div', 'seg', ...btns);
}

export function button(text: string, onClick: () => void, className = 'btn', title?: string): HTMLButtonElement {
  const b = h('button', className, text);
  b.type = 'button';
  if (title) b.title = title;
  b.addEventListener('click', onClick);
  return b;
}

export function iconButton(name: IconName, title: string, onClick: (e: MouseEvent) => void, size = 14, className = 'icon-btn'): HTMLButtonElement {
  const b = h('button', className);
  b.type = 'button';
  b.title = title;
  b.append(icon(name, size));
  b.addEventListener('click', onClick);
  return b;
}

export type { Field };
export { formatNumber };
