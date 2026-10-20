// Controls generated from a shader's ISF INPUTS, grouped by "Group: Setting" labels.

import { defaultValues, parseIsf, type InputValue, type IsfInput } from '../../isf/parse';
import type { ShaderContent } from '../../state/types';
import { h, hexToRgb, rgbToHex } from '../dom';
import { numberField } from '../widgets/numberField';
import { colorControl, labelled, resetButton, selectControl, Section, sliderRow, toggleControl, type Row } from '../widgets/rows';

export const CONTROL_TYPES = ['float', 'long', 'bool', 'color', 'point2D'];

export function shaderInputs(source: string): IsfInput[] {
  try {
    return parseIsf(source).meta.inputs.filter((i) => CONTROL_TYPES.includes(i.type));
  } catch {
    return [];
  }
}

export interface ShaderAccess {
  content(): ShaderContent;
  /** merge = part of a drag (one undo step). */
  setValue(name: string, v: InputValue, merge: boolean): void;
  setAllValues(values: Record<string, InputValue>): void;
}

function niceStep(min: number, max: number): number {
  const raw = (max - min) / 200;
  if (!(raw > 0)) return 0.01;
  const pow = Math.pow(10, Math.floor(Math.log10(raw)));
  return raw / pow >= 5 ? pow * 5 : raw / pow >= 2 ? pow * 2 : pow;
}

const same = (a: InputValue, b: InputValue) => (Array.isArray(a) && Array.isArray(b) ? a.length === b.length && a.every((v, i) => v === b[i]) : a === b);

/** Adds a reset button at the end of a row; it only shows when the value differs from the default. */
function withReset(row: Row, isDefault: () => boolean, reset: () => void): Row {
  const btn = resetButton(reset);
  row.el.append(btn);
  return {
    el: row.el,
    sync: () => {
      row.sync();
      btn.style.visibility = isDefault() ? 'hidden' : 'visible';
    },
  };
}

function controlFor(input: IsfInput, acc: ShaderAccess): Row {
  const label = input.shortLabel;
  const val = () => acc.content().values[input.name] ?? input.default;
  const def = input.default;
  const reset = () => acc.setValue(input.name, Array.isArray(def) ? [...def] : def, false);
  const tip = input.label;

  switch (input.type) {
    case 'float': {
      const min = input.min ?? 0;
      const max = input.max ?? 1;
      const step = niceStep(min, max);
      const decimals = Math.min(4, Math.max(0, Math.ceil(-Math.log10(step) - 1e-9)));
      return sliderRow({
        label,
        tip,
        stacked: true,
        min,
        max,
        step,
        decimals,
        def: def as number,
        get: () => Number(val()),
        set: (v, merge) => acc.setValue(input.name, v, merge),
      });
    }
    case 'long': {
      const values = input.values ?? [];
      const labels = input.labels ?? [];
      const c = selectControl(
        values.map((v, i) => ({ value: String(v), label: labels[i] ?? String(v) })),
        () => String(val()),
        (v) => acc.setValue(input.name, parseInt(v, 10), false),
      );
      return withReset({ el: labelled(label, c.el, tip), sync: c.sync }, () => same(val(), def), reset);
    }
    case 'bool': {
      const c = toggleControl(
        () => Boolean(val()),
        (v) => acc.setValue(input.name, v, false),
      );
      return withReset({ el: labelled(label, c.el, tip), sync: c.sync }, () => same(val(), def), reset);
    }
    case 'color': {
      const toHex = () => {
        const a = val() as number[];
        return rgbToHex((a[0] ?? 0) * 255, (a[1] ?? 0) * 255, (a[2] ?? 0) * 255);
      };
      const c = colorControl(toHex, (hex, merge) => {
        const [r, g, b] = hexToRgb(hex);
        const alpha = (val() as number[])[3] ?? 1;
        acc.setValue(input.name, [r / 255, g / 255, b / 255, alpha], merge);
      });
      return withReset({ el: labelled(label, c.el, tip), sync: c.sync }, () => same(val(), def), reset);
    }
    default: {
      // point2D
      const pt = () => val() as number[];
      const big = (def as number[]).some((n) => Math.abs(n) > 2);
      const step = big ? 1 : 0.01;
      const mk = (i: 0 | 1, name: string) =>
        numberField({
          label: name,
          step,
          get: () => pt()[i] ?? 0,
          set: (v, merge) => {
            const next = [...pt()];
            next[i] = v;
            acc.setValue(input.name, next, merge);
          },
        });
      const x = mk(0, 'X');
      const y = mk(1, 'Y');
      const row: Row = {
        el: labelled(label, h('div', 'pair', x.el, y.el), tip),
        sync: () => {
          x.sync();
          y.sync();
        },
      };
      return withReset(row, () => same(val(), def), reset);
    }
  }
}

/** Builds all controls; returns a section plus a function that resets every value. */
export function buildShaderControls(source: string, acc: ShaderAccess): { section: Section; count: number; resetAll(): void } {
  const inputs = shaderInputs(source);
  const section = new Section('shader-controls');
  let lastGroup: string | null | undefined;
  for (const input of inputs) {
    if (input.group !== lastGroup) {
      lastGroup = input.group;
      if (input.group) section.addEl(h('div', 'group-label', input.group));
    }
    section.add(controlFor(input, acc));
  }
  return { section, count: inputs.length, resetAll: () => acc.setAllValues(defaultValues(inputs)) };
}
