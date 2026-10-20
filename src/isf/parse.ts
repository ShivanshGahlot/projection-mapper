// Parses the JSON header of an ISF fragment shader and normalises its INPUTS.

export type InputValue = number | boolean | number[];

export interface IsfInput {
  name: string;
  /** Raw ISF type: float, long, bool, event, color, point2D, image, ... */
  type: string;
  /** Full LABEL (falls back to NAME). */
  label: string;
  /** Text before the colon in a "Group: Setting" label, or null. */
  group: string | null;
  /** Label with the group prefix removed. */
  shortLabel: string;
  default: InputValue;
  min?: number;
  max?: number;
  /** long only: the allowed values and their display labels. */
  values?: number[];
  labels?: string[];
}

export interface IsfMeta {
  description: string;
  credit: string;
  categories: string[];
  inputs: IsfInput[];
}

export interface ParsedIsf {
  meta: IsfMeta;
  /** GLSL body. The header is replaced by blank lines so line numbers match the file. */
  body: string;
}

export class IsfParseError extends Error {}

const HEADER_RE = /^\s*\/\*([\s\S]*?)\*\//;

export function parseIsf(source: string): ParsedIsf {
  const m = HEADER_RE.exec(source);
  if (!m || !m[1].trim().startsWith('{')) {
    throw new IsfParseError('No ISF header found. The file must start with a /* { ... } */ JSON block.');
  }
  const json = parseJsonLenient(m[1]);
  if (typeof json !== 'object' || json === null || Array.isArray(json)) {
    throw new IsfParseError('The ISF header must be a JSON object.');
  }
  const header = json as Record<string, unknown>;

  const rawInputs = Array.isArray(header.INPUTS) ? header.INPUTS : [];
  const inputs: IsfInput[] = [];
  const seen = new Set<string>();
  for (const raw of rawInputs) {
    const input = normaliseInput(raw);
    if (input && !seen.has(input.name)) {
      seen.add(input.name);
      inputs.push(input);
    }
  }

  return {
    meta: {
      description: typeof header.DESCRIPTION === 'string' ? header.DESCRIPTION : '',
      credit: typeof header.CREDIT === 'string' ? header.CREDIT : '',
      categories: Array.isArray(header.CATEGORIES) ? header.CATEGORIES.filter((c): c is string => typeof c === 'string') : [],
      inputs,
    },
    body: m[0].replace(/[^\n]/g, '') + source.slice(m[0].length),
  };
}

function parseJsonLenient(text: string): unknown {
  try {
    return JSON.parse(text);
  } catch (strictError) {
    // Many hand-written ISF headers have trailing commas.
    try {
      return JSON.parse(text.replace(/,(\s*[}\]])/g, '$1'));
    } catch {
      throw new IsfParseError(`The ISF header is not valid JSON: ${(strictError as Error).message}`);
    }
  }
}

function num(v: unknown, fallback: number): number {
  const n = typeof v === 'number' ? v : typeof v === 'string' ? Number(v) : NaN;
  return Number.isFinite(n) ? n : fallback;
}

function numArray(v: unknown, fallback: number[]): number[] {
  if (!Array.isArray(v)) return fallback;
  return fallback.map((f, i) => num(v[i], f));
}

function normaliseInput(raw: unknown): IsfInput | null {
  if (typeof raw !== 'object' || raw === null) return null;
  const r = raw as Record<string, unknown>;
  if (typeof r.NAME !== 'string' || !/^[A-Za-z_]\w*$/.test(r.NAME) || typeof r.TYPE !== 'string') return null;

  const label = typeof r.LABEL === 'string' && r.LABEL.trim() ? r.LABEL.trim() : r.NAME;
  const colon = label.indexOf(':');
  const grouped = colon > 0 && colon < label.length - 1;
  const base = {
    name: r.NAME,
    type: r.TYPE,
    label,
    group: grouped ? label.slice(0, colon).trim() : null,
    shortLabel: grouped ? label.slice(colon + 1).trim() : label,
  };

  switch (r.TYPE) {
    case 'float': {
      let min = num(r.MIN, 0);
      let max = num(r.MAX, 1);
      if (min > max) [min, max] = [max, min];
      if (min === max) max = min + 1;
      const def = Math.min(max, Math.max(min, num(r.DEFAULT, min)));
      return { ...base, min, max, default: def };
    }
    case 'long': {
      let values = Array.isArray(r.VALUES) ? r.VALUES.map((v) => Math.round(num(v, 0))) : [];
      if (values.length === 0) {
        const min = Math.round(num(r.MIN, 0));
        const max = Math.round(num(r.MAX, min));
        for (let v = min; v <= max && values.length < 1000; v++) values.push(v);
      }
      if (values.length === 0) values = [0];
      const rawLabels = Array.isArray(r.LABELS) ? r.LABELS : [];
      const labels = values.map((v, i) => (rawLabels[i] != null ? String(rawLabels[i]) : String(v)));
      const def = Math.round(num(r.DEFAULT, values[0]));
      return { ...base, values, labels, default: values.includes(def) ? def : values[0] };
    }
    case 'bool':
    case 'event':
      return { ...base, default: r.DEFAULT === true || num(r.DEFAULT, 0) !== 0 };
    case 'color':
      return { ...base, default: numArray(r.DEFAULT, [0, 0, 0, 1]) };
    case 'point2D':
      return { ...base, default: numArray(r.DEFAULT, [0, 0]) };
    default:
      // image, audio, audioFFT and anything unknown: declared, but not adjustable yet.
      return { ...base, default: 0 };
  }
}

export function defaultValues(inputs: IsfInput[]): Record<string, InputValue> {
  const out: Record<string, InputValue> = {};
  for (const i of inputs) out[i.name] = Array.isArray(i.default) ? [...i.default] : i.default;
  return out;
}

/** Input types that get a control in the UI. */
export function isAdjustable(input: IsfInput): boolean {
  return input.type === 'float' || input.type === 'long' || input.type === 'bool';
}
