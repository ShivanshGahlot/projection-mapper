// Acceptance test for the ISF loader: compiles every library shader, checks that it
// animates and that every control visibly changes the image. Results go to #results as JSON.

import { shaderLibrary } from './shaderLibrary';
import { library } from './library';
import { builtinManifest } from './library/manifest';
import { parseIsf, defaultValues, isAdjustable } from './isf/parse';
import type { InputValue, IsfInput, ParsedIsf } from './isf/parse';
import { IsfProgram } from './isf/compile';
import type { HostValues } from './isf/compile';
import { RenderTarget } from './gl/renderTarget';
import { drawQuad } from './gl/quad';

const TIME = 7.3;
const TIME_B = 8.1;
const EARLY_TIME = 0.5;
const LONG_SIDE = 480;
const SQUARE = 480;
const CHANNEL_TOLERANCE = 2;
const ANIMATE_MIN_FRACTION = 0.005;
/** A control passes when an alternative value changes at least this fraction of pixels. */
const CONTROL_MIN_FRACTION = 0.0002;
const CONTROL_MIN_PIXELS = 5;

type Values = Record<string, InputValue>;

interface ControlResult {
  name: string;
  label: string;
  type: string;
  pass: boolean;
  maxChangedPixels: number;
  maxChangedPct: number;
  /** Context that made the control pass ('default' if no retry was needed), or the best failed one. */
  context: string;
  bestAlternative: string;
}

interface ShaderResult {
  fileName: string;
  compiled: boolean;
  error: string;
  width: number;
  height: number;
  /** Static shaders (marked in the library manifest) skip the animation check. */
  static?: boolean;
  animates?: { nativePct: number; squarePct: number; pass: boolean };
  controls: ControlResult[];
  pass: boolean;
}

const canvas = document.createElement('canvas');
canvas.width = canvas.height = 1;
const gl = canvas.getContext('webgl2', { antialias: false, preserveDrawingBuffer: true })!;
const target = new RenderTarget(gl);

const sleep = (ms: number) => new Promise<void>((r) => setTimeout(r, ms));

async function compile(parsed: ParsedIsf): Promise<IsfProgram> {
  const prog = new IsfProgram(gl, parsed);
  const t0 = performance.now();
  while (prog.poll() === 'pending' && performance.now() - t0 < 60000) await sleep(0);
  return prog;
}

function hostValues(time: number, w: number, h: number): HostValues {
  return {
    time, timeDelta: 1 / 60, frameIndex: 440, renderWidth: w, renderHeight: h,
    date: [2026, 10, 20, 43200],
    audioBass: 0.6, audioMid: 0.5, audioHigh: 0.4, audioBeat: 1, audioLevel: 0.5,
  };
}

function render(prog: IsfProgram, values: Values, time: number, w: number, h: number): Uint8Array {
  target.ensureSize(w, h);
  target.bind();
  gl.disable(gl.BLEND);
  gl.clearColor(0, 0, 0, 0);
  gl.clear(gl.COLOR_BUFFER_BIT);
  prog.use(hostValues(time, w, h), values);
  drawQuad(gl);
  const out = new Uint8Array(w * h * 4);
  gl.readPixels(0, 0, w, h, gl.RGBA, gl.UNSIGNED_BYTE, out);
  return out;
}

function countDiff(a: Uint8Array, b: Uint8Array): number {
  let n = 0;
  for (let i = 0; i < a.length; i += 4) {
    if (
      Math.abs(a[i] - b[i]) > CHANNEL_TOLERANCE || Math.abs(a[i + 1] - b[i + 1]) > CHANNEL_TOLERANCE ||
      Math.abs(a[i + 2] - b[i + 2]) > CHANNEL_TOLERANCE || Math.abs(a[i + 3] - b[i + 3]) > CHANNEL_TOLERANCE
    ) n++;
  }
  return n;
}

/** Native aspect from the shader's CANVAS constant or pixel-size expression; square if unknown. */
function nativeSize(source: string): [number, number] {
  let aspect = 1;
  const px = /max\(\s*([\d.]+)\s*\/\s*RENDERSIZE\.x\s*,\s*([\d.]+)\s*\/\s*RENDERSIZE\.y\s*\)/.exec(source);
  const canvasConst = /const\s+vec2\s+CANVAS\s*=\s*vec2\(\s*([\d.]+)\s*,\s*([\d.]+)\s*\)/.exec(source);
  const m = canvasConst ?? px;
  if (m) aspect = Number(m[1]) / Number(m[2]);
  return aspect >= 1 ? [LONG_SIDE, Math.round(LONG_SIDE / aspect)] : [Math.round(LONG_SIDE * aspect), LONG_SIDE];
}

/** Colour inputs are tested too, with a contrasting colour (each channel pushed to its far extreme). */
function isTestable(input: IsfInput): boolean {
  return isAdjustable(input) || input.type === 'color';
}

function alternatives(input: IsfInput, current: InputValue): InputValue[] {
  if (input.type === 'color') {
    const c = current as number[];
    return [[c[0] < 0.5 ? 1 : 0, c[1] < 0.5 ? 1 : 0, c[2] < 0.5 ? 1 : 0, c[3] ?? 1]];
  }
  if (input.type === 'bool') return [!current];
  if (input.type === 'long') return (input.values ?? []).filter((v) => v !== current);
  return [input.min!, input.max!].filter((v) => v !== current);
}

function fmt(v: InputValue): string {
  if (Array.isArray(v)) return `[${v.map((n) => Math.round(n * 100) / 100).join(',')}]`;
  return typeof v === 'number' ? String(Math.round(v * 1000) / 1000) : String(v);
}

interface Trial { pixels: number; alt: string }

/** Renders the shader with the control at each alternative value and returns the biggest change. */
function tryControl(
  prog: IsfProgram, input: IsfInput, base: Values, time: number, w: number, h: number,
  cache: { img?: Uint8Array },
): Trial {
  cache.img ??= render(prog, base, time, w, h);
  let best: Trial = { pixels: 0, alt: '-' };
  for (const alt of alternatives(input, base[input.name])) {
    const img = render(prog, { ...base, [input.name]: alt }, time, w, h);
    const pixels = countDiff(cache.img, img);
    if (pixels >= best.pixels) best = { pixels, alt: fmt(alt) };
  }
  return best;
}

function isCalibration(i: IsfInput): boolean {
  return /calib/i.test(i.label) || /calib/i.test(i.name);
}

async function testControls(prog: IsfProgram, parsed: ParsedIsf, w: number, h: number): Promise<ControlResult[]> {
  const inputs = parsed.meta.inputs.filter(isTestable);
  const defaults = defaultValues(parsed.meta.inputs);
  const total = w * h;
  const need = Math.max(CONTROL_MIN_PIXELS, Math.ceil(total * CONTROL_MIN_FRACTION));
  const longs = inputs.filter((i) => i.type === 'long');

  const boolsOn: Values = { ...defaults };
  for (const i of inputs) if (i.type === 'bool' && !isCalibration(i)) boolsOn[i.name] = true;

  // Contexts are tried in order; the first one that makes the control pass wins.
  interface Ctx { label: string; values: Values; time: number }
  const contexts: Ctx[] = [
    { label: 'default', values: defaults, time: TIME },
    { label: 'bools-on', values: boolsOn, time: TIME },
  ];
  // Some controls only act in part of a shader's looping timeline (start-up, end of a cycle, later cycles).
  for (const t of [EARLY_TIME, 10.8, 12.5, 20]) contexts.push({ label: `time=${t}`, values: defaults, time: t });
  for (const l of longs) {
    for (const v of l.values!) contexts.push({ label: `long:${l.name}=${v}`, values: { ...defaults, [l.name]: v }, time: TIME });
  }
  for (const l of longs) {
    for (const v of l.values!) contexts.push({ label: `bools-on+long:${l.name}=${v}`, values: { ...boolsOn, [l.name]: v }, time: TIME });
  }

  const caches: { img?: Uint8Array }[] = contexts.map(() => ({}));
  const results: ControlResult[] = [];
  for (const input of inputs) {
    let best: Trial = { pixels: -1, alt: '-' };
    let bestCtx = 'default';
    let pass = false;
    for (let c = 0; c < contexts.length; c++) {
      const ctx = contexts[c];
      const trial = tryControl(prog, input, ctx.values, ctx.time, w, h, caches[c]);
      if (trial.pixels > best.pixels) {
        best = trial;
        bestCtx = ctx.label;
      }
      if (trial.pixels >= need) {
        pass = true;
        break;
      }
    }
    results.push({
      name: input.name, label: input.label, type: input.type, pass,
      maxChangedPixels: best.pixels, maxChangedPct: Math.round((best.pixels / total) * 10000) / 100,
      context: bestCtx, bestAlternative: best.alt,
    });
    await sleep(0);
  }
  return results;
}

function thumbnail(pixels: Uint8Array, w: number, h: number, label: string): HTMLElement {
  const full = document.createElement('canvas');
  full.width = w;
  full.height = h;
  const img = new ImageData(w, h);
  for (let y = 0; y < h; y++) {
    img.data.set(pixels.subarray((h - 1 - y) * w * 4, (h - y) * w * 4), y * w * 4);
  }
  for (let i = 3; i < img.data.length; i += 4) img.data[i] = 255;
  full.getContext('2d')!.putImageData(img, 0, 0);
  const scale = Math.min(1, 240 / Math.max(w, h));
  const thumb = document.createElement('canvas');
  thumb.width = Math.round(w * scale);
  thumb.height = Math.round(h * scale);
  const ctx = thumb.getContext('2d')!;
  ctx.imageSmoothingQuality = 'high';
  ctx.drawImage(full, 0, 0, thumb.width, thumb.height);
  const fig = document.createElement('figure');
  const cap = document.createElement('figcaption');
  cap.textContent = label;
  fig.append(thumb, cap);
  return fig;
}

/** Deliberately breaks the first shader and checks the error log points at the right line. */
async function brokenShaderCase() {
  const src = shaderLibrary[0].source;
  const idx = src.indexOf('void main');
  const line = src.slice(0, idx).split('\n').length;
  const broken = src.slice(0, idx) + 'void main(' + src.slice(idx + 'void main'.length);
  const prog = await compile(parseIsf(broken));
  const log = prog.error;
  const lines = [...log.matchAll(/ERROR:\s*\d+:(\d+)/g)].map((m) => Number(m[1]));
  const state = prog.poll();
  const pass = state === 'error' && log.length > 0 && lines.some((l) => Math.abs(l - line) <= 1);
  return { shader: shaderLibrary[0].fileName, expectedLine: line, state, reportedLines: lines, log, pass };
}

async function run() {
  const t0 = performance.now();
  const sheet = document.getElementById('sheet')!;
  const shaders: ShaderResult[] = [];
  const dbg = gl.getExtension('WEBGL_debug_renderer_info');
  const renderer = dbg ? String(gl.getParameter(dbg.UNMASKED_RENDERER_WEBGL)) : 'unknown';

  const staticIds = new Set(builtinManifest.filter((b) => b.static).map((b) => `builtin/${b.slug}`));
  for (const lib of library.shaders) {
    const [w, h] = nativeSize(lib.source);
    const result: ShaderResult = { fileName: lib.id, static: staticIds.has(lib.id), compiled: false, error: '', width: w, height: h, controls: [], pass: false };
    shaders.push(result);
    try {
      const parsed = parseIsf(lib.source);
      const prog = await compile(parsed);
      if (prog.poll() !== 'ready') {
        result.error = prog.error;
      } else {
        result.compiled = true;
        const defaults = defaultValues(parsed.meta.inputs);
        const base = render(prog, defaults, TIME, w, h);
        const later = render(prog, defaults, TIME_B, w, h);
        const nativePct = (countDiff(base, later) / (w * h)) * 100;
        const sqA = render(prog, defaults, TIME, SQUARE, SQUARE);
        const sqB = render(prog, defaults, TIME_B, SQUARE, SQUARE);
        const squarePct = (countDiff(sqA, sqB) / (SQUARE * SQUARE)) * 100;
        result.animates = {
          nativePct: Math.round(nativePct * 100) / 100,
          squarePct: Math.round(squarePct * 100) / 100,
          pass: !!result.static || nativePct / 100 > ANIMATE_MIN_FRACTION && squarePct / 100 > ANIMATE_MIN_FRACTION,
        };
        sheet.append(thumbnail(base, w, h, `${lib.id} (${w}x${h})`));
        result.controls = await testControls(prog, parsed, w, h);
        prog.dispose();
      }
    } catch (e) {
      result.error = String(e);
    }
    result.pass = result.compiled && !!result.animates?.pass && result.controls.every((c) => c.pass);
    document.getElementById('status')!.textContent = `Running ISF selftest... ${shaders.length}/${library.shaders.length}`;
    await sleep(0);
  }

  const broken = await brokenShaderCase();
  const pass = shaders.every((s) => s.pass) && broken.pass;
  const out = {
    pass, renderer, seconds: Math.round((performance.now() - t0) / 100) / 10,
    time: TIME, timeB: TIME_B,
    thresholds: {
      channelTolerance: CHANNEL_TOLERANCE, animateMinFraction: ANIMATE_MIN_FRACTION,
      controlMinFraction: CONTROL_MIN_FRACTION, controlMinPixels: CONTROL_MIN_PIXELS,
    },
    shaders, broken,
  };

  const rows = shaders.map((s) => {
    const failing = s.controls.filter((c) => !c.pass).map((c) => c.label).join('; ');
    return `<tr><td>${s.fileName}</td><td class="${s.compiled ? 'ok' : 'bad'}">${s.compiled ? 'ok' : 'FAIL'}</td>` +
      `<td class="${s.animates?.pass ? 'ok' : 'bad'}">${s.animates ? s.animates.nativePct + '%' : '-'}</td>` +
      `<td>${s.controls.length}</td><td class="${failing ? 'bad' : 'ok'}">${failing || 'all pass'}</td></tr>`;
  });
  document.getElementById('summary')!.innerHTML =
    `<table><tr><th>Shader</th><th>Compile</th><th>Animates</th><th>Controls</th><th>Failing controls</th></tr>${rows.join('')}</table>` +
    `<p class="${broken.pass ? 'ok' : 'bad'}">Broken-shader case: ${broken.pass ? 'pass' : 'FAIL'} (expected line ${broken.expectedLine}, reported ${broken.reportedLines.join(',')})</p>`;
  document.getElementById('results')!.textContent = JSON.stringify(out, null, 1);
  document.getElementById('status')!.textContent = `ISF selftest ${pass ? 'PASSED' : 'FAILED'} (${renderer})`;
  document.title = 'SELFTEST DONE';
}

run().catch((e) => {
  document.getElementById('results')!.textContent = JSON.stringify({ pass: false, fatal: String(e) });
  document.title = 'SELFTEST DONE';
});
