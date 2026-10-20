// Builds the controls for one layer from its shader's INPUTS. The panel is only
// rebuilt when the shader changes; value changes write straight into the store.

import { defaultValues, isAdjustable, parseIsf, IsfParseError, type InputValue, type IsfInput } from '../isf/parse';
import type { LayerStatus } from '../layers/layer';
import type { Store } from '../state/store';
import type { LayerState, ProjectState } from '../state/types';

export interface Control {
  el: HTMLElement;
  /** Re-reads the current value into the widgets (used after a reset). */
  refresh(): void;
  reset(): void;
}

function el<K extends keyof HTMLElementTagNameMap>(
  tag: K,
  className?: string,
  text?: string,
): HTMLElementTagNameMap[K] {
  const e = document.createElement(tag);
  if (className) e.className = className;
  if (text !== undefined) e.textContent = text;
  return e;
}

/** A slider step that gives roughly 200 stops across the range, as a round number. */
function niceStep(min: number, max: number): number {
  const raw = (max - min) / 200;
  if (!(raw > 0)) return 0.01;
  const pow = Math.pow(10, Math.floor(Math.log10(raw)));
  return raw / pow >= 5 ? pow * 5 : raw / pow >= 2 ? pow * 2 : pow;
}

function decimals(step: number): number {
  return Math.min(4, Math.max(0, Math.ceil(-Math.log10(step) - 1e-9)));
}

function resetButton(onClick: () => void): HTMLButtonElement {
  const b = el('button', 'reset', '↺');
  b.type = 'button';
  b.title = 'Reset to default';
  b.addEventListener('click', onClick);
  return b;
}

export interface Binding<T> {
  label: string;
  def: T;
  get(): T;
  set(v: T): void;
}

function row(label: string, field: HTMLElement, reset: HTMLButtonElement): HTMLElement {
  const r = el('div', 'control');
  r.append(el('label', 'control-label', label), field, reset);
  return r;
}

export function sliderControl(b: Binding<number>, min: number, max: number, step?: number): Control {
  step = step ?? niceStep(min, max);
  const places = decimals(step);
  const range = el('input');
  range.type = 'range';
  range.min = String(min);
  range.max = String(max);
  range.step = String(step);
  const num = el('input', 'num');
  num.type = 'number';
  num.min = String(min);
  num.max = String(max);
  num.step = String(step);
  const field = el('div', 'field');
  field.append(range, num);

  const reset = resetButton(() => ctl.reset());
  const refresh = () => {
    const v = b.get();
    range.value = String(v);
    num.value = String(Number(v.toFixed(places)));
    reset.disabled = v === b.def;
  };
  const commit = (v: number) => {
    if (!Number.isFinite(v)) return refresh();
    b.set(Math.min(max, Math.max(min, v)));
    refresh();
  };
  range.addEventListener('input', () => commit(range.valueAsNumber));
  num.addEventListener('change', () => commit(num.valueAsNumber));
  const ctl: Control = {
    el: row(b.label, field, reset),
    refresh,
    reset: () => commit(b.def),
  };
  refresh();
  return ctl;
}

function selectControl(b: Binding<number>, values: number[], labels: string[]): Control {
  const select = el('select');
  values.forEach((v, i) => {
    const o = el('option', undefined, labels[i] ?? String(v));
    o.value = String(v);
    select.append(o);
  });
  const reset = resetButton(() => ctl.reset());
  const refresh = () => {
    select.value = String(b.get());
    reset.disabled = b.get() === b.def;
  };
  select.addEventListener('change', () => {
    b.set(parseInt(select.value, 10));
    refresh();
  });
  const ctl: Control = {
    el: row(b.label, select, reset),
    refresh,
    reset: () => {
      b.set(b.def);
      refresh();
    },
  };
  refresh();
  return ctl;
}

function toggleControl(b: Binding<boolean>): Control {
  const sw = el('label', 'switch');
  const box = el('input');
  box.type = 'checkbox';
  sw.append(box, el('span', 'slider'));
  const reset = resetButton(() => ctl.reset());
  const refresh = () => {
    box.checked = b.get();
    reset.disabled = b.get() === b.def;
  };
  box.addEventListener('change', () => {
    b.set(box.checked);
    refresh();
  });
  const ctl: Control = {
    el: row(b.label, sw, reset),
    refresh,
    reset: () => {
      b.set(b.def);
      refresh();
    },
  };
  refresh();
  return ctl;
}

export class ControlsPanel {
  private layerId: string | null = null;
  private statusBox = el('div', 'status');
  private shaderControls: Control[] = [];

  constructor(
    private readonly root: HTMLElement,
    private readonly store: Store<ProjectState>,
  ) {}

  private layer(): LayerState | undefined {
    return this.store.state.layers.find((l) => l.id === this.layerId);
  }

  /** Rebuilds the panel for a layer. Call when the layer or its shader changes. */
  show(layerId: string | null): void {
    this.layerId = layerId;
    this.root.replaceChildren();
    this.shaderControls = [];
    this.statusBox = el('div', 'status');
    this.statusBox.hidden = true;
    const layer = this.layer();
    if (!layer) return;

    let inputs: IsfInput[] = [];
    let description = '';
    try {
      const parsed = parseIsf(layer.source);
      inputs = parsed.meta.inputs;
      description = parsed.meta.description;
    } catch (e) {
      if (!(e instanceof IsfParseError)) throw e;
    }

    this.root.append(this.statusBox);
    if (description) this.root.append(el('p', 'description', description));
    this.root.append(this.buildPlayback(layer));
    this.root.append(this.buildShaderControls(layer, inputs));
  }

  setStatus(layerId: string, status: LayerStatus): void {
    if (layerId !== this.layerId) return;
    const box = this.statusBox;
    box.className = 'status';
    box.replaceChildren();
    if (status.state === 'ok') {
      box.hidden = true;
      return;
    }
    box.hidden = false;
    if (status.state === 'loading') {
      box.classList.add('loading');
      box.textContent = 'Compiling…';
    } else {
      box.classList.add('error');
      box.append(el('strong', undefined, 'This shader has an error and is not drawing.'), el('pre', undefined, status.message));
    }
  }

  private edit(mutate: (layer: LayerState) => void): void {
    this.store.update(() => {
      const layer = this.layer();
      if (layer) mutate(layer);
    });
  }

  private buildPlayback(layer: LayerState): HTMLElement {
    const section = el('section', 'group');
    section.append(el('h3', undefined, 'Playback'));
    section.append(
      sliderControl(
        { label: 'Playback speed', def: 1, get: () => layer.speed, set: (v) => this.edit((l) => (l.speed = v)) },
        0,
        4,
        0.05,
      ).el,
      toggleControl({ label: 'Pause', def: false, get: () => layer.paused, set: (v) => this.edit((l) => (l.paused = v)) }).el,
      sliderControl(
        { label: 'Time offset (seconds)', def: 0, get: () => layer.timeOffset, set: (v) => this.edit((l) => (l.timeOffset = v)) },
        -600,
        600,
        0.1,
      ).el,
    );
    return section;
  }

  private buildShaderControls(layer: LayerState, inputs: IsfInput[]): HTMLElement {
    const wrap = el('div');
    const defaults = defaultValues(inputs);
    const header = el('div', 'section-head');
    header.append(el('h3', undefined, 'Shader controls'));
    const resetAll = el('button', 'text-button', 'Reset all controls');
    resetAll.type = 'button';
    resetAll.addEventListener('click', () => {
      for (const c of this.shaderControls) c.reset();
    });
    header.append(resetAll);
    wrap.append(header);

    const groups = new Map<string, HTMLElement>();
    const unsupported: IsfInput[] = [];
    for (const input of inputs) {
      if (!isAdjustable(input)) {
        unsupported.push(input);
        continue;
      }
      const ctl = this.buildInputControl(layer, input, defaults[input.name]);
      if (!ctl) continue;
      this.shaderControls.push(ctl);
      let parent: HTMLElement = wrap;
      if (input.group) {
        let g = groups.get(input.group);
        if (!g) {
          g = el('section', 'group');
          g.append(el('h4', undefined, input.group));
          groups.set(input.group, g);
          wrap.append(g);
        }
        parent = g;
      }
      parent.append(ctl.el);
    }

    if (this.shaderControls.length === 0) wrap.append(el('p', 'muted', 'This shader has no adjustable controls.'));
    if (unsupported.length > 0) {
      const g = el('section', 'group');
      g.append(el('h4', undefined, 'Not adjustable yet'));
      for (const u of unsupported) g.append(el('div', 'unsupported', `${u.label} (${u.type})`));
      wrap.append(g);
    }
    return wrap;
  }

  private buildInputControl(layer: LayerState, input: IsfInput, def: InputValue): Control | null {
    const label = input.group ? input.shortLabel : input.label;
    const base = {
      label,
      get: () => layer.values[input.name],
      set: (v: InputValue) => this.edit((l) => (l.values[input.name] = v)),
    };
    if (input.type === 'bool') {
      return toggleControl({ ...base, def: !!def, get: () => !!base.get() });
    }
    if (input.type === 'long') {
      const values = input.values ?? [];
      if (values.length === 0) return null;
      return selectControl({ ...base, def: Number(def), get: () => Number(base.get()) }, values, input.labels ?? []);
    }
    const min = input.min ?? 0;
    const max = input.max ?? 1;
    return sliderControl({ ...base, def: Number(def), get: () => Number(base.get()) }, min, max);
  }
}
