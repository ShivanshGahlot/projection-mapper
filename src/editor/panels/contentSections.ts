// Property sections for each kind of layer content: surface fill, shader,
// text, webcam, and the mask settings.

import { library } from '../../library/index';
import { assetStorage } from '../../persist/db';
import { activeScene, shaderContent } from '../../state/defaults';
import type { ContentLayer, Fill, GradientStop, MaskLayer, ShaderContent, SurfaceContent, TextContent, WebcamContent } from '../../state/types';
import type { Ctx } from '../ctx';
import { h } from '../dom';
import { findLayer, updateLayer } from '../layerOps';
import { buildShaderControls } from './shaderControls';
import { hooks } from '../widgets/numberField';
import { pickFile } from '../widgets/overlays';
import {
  button,
  colorRow,
  dynamicSection,
  iconButton,
  labelled,
  numberRow,
  pair,
  Section,
  sectionLabel,
  segmented,
  selectControl,
  selectRow,
  sliderRow,
  toggleButton,
  toggleRow,
  type Row,
} from '../widgets/rows';

type MergeFn = (v: number, merge: boolean) => void;

/** Shared helpers bound to one layer id. */
export function binder(ctx: Ctx, id: string) {
  const layer = () => findLayer(ctx.store.state, id) as ContentLayer;
  const edit = (fn: (l: ContentLayer) => void, mergeKey?: string) => updateLayer<ContentLayer>(ctx, id, fn, mergeKey);
  const content = <T extends ContentLayer['content']>() => layer().content as T;
  /** Setter for numbers: scrubs merge into one undo step. */
  const num = (key: string, fn: (l: ContentLayer, v: number) => void): MergeFn => (v, merge) => edit((l) => fn(l, v), merge ? key : undefined);
  return { layer, edit, content, num };
}
export type Binder = ReturnType<typeof binder>;

// ---------- Surface ----------

function fillLabel(f: Fill): string {
  switch (f.type) {
    case 'none': return 'None';
    case 'grid': return 'Grid';
    case 'color': return f.color;
    case 'gradient': return f.kind === 'linear' ? 'Linear Gradient' : 'Radial Gradient';
    default: return f.fileName;
  }
}

export function buildSurface(_ctx: Ctx, b: Binder): Section {
  const s = new Section();
  const surf = () => b.content<SurfaceContent>();
  const setFill = (fill: Fill) => b.edit((l) => { (l.content as SurfaceContent).fill = fill; });

  const field = h('div', 'fill-field');
  const fieldRow: Row = { el: labelled('Fill', field, 'What this surface is filled with'), sync: () => { field.textContent = fillLabel(surf().fill); } };
  fieldRow.sync();

  const mk = (name: Parameters<typeof iconButton>[0], title: string, type: Fill['type'], run: () => void) => {
    const btn = iconButton(name, title, run, 15, 'icon-btn fill-btn');
    return { btn, sync: () => btn.classList.toggle('active', surf().fill.type === type) };
  };
  const pickMedia = async (accept: string, kind: 'image' | 'video') => {
    const file = await pickFile(accept);
    if (!file) return;
    const assetId = await assetStorage.put(file, file.name);
    setFill(kind === 'image' ? { type: 'image', assetId, fileName: file.name } : { type: 'video', assetId, fileName: file.name, loop: true, playbackRate: 1 });
  };
  const gridBtn = mk('grid', 'Alignment grid (for lining the shape up with your object)', 'grid', () => setFill({ type: 'grid' }));
  const colorBtn = mk('droplet', 'Solid colour', 'color', () => {
    const f = surf().fill;
    if (f.type !== 'color') setFill({ type: 'color', color: '#ffffff' });
  });
  const gradBtn = mk('gradient', 'Gradient', 'gradient', () => {
    if (surf().fill.type !== 'gradient') setFill({ type: 'gradient', kind: 'linear', angle: 90, stops: [{ pos: 0, color: '#3b82f6' }, { pos: 1, color: '#ec4899' }] });
  });
  const imgBtn = mk('image', 'Image from a file...', 'image', () => void pickMedia('image/*', 'image'));
  const vidBtn = mk('video', 'Video from a file...', 'video', () => void pickMedia('video/*', 'video'));
  const noneBtn = iconButton('x', 'No fill (transparent)', () => setFill({ type: 'none' }), 15, 'icon-btn fill-btn');
  const buttons = [gridBtn, colorBtn, gradBtn, imgBtn, vidBtn];
  const btnRow: Row = {
    el: h('div', 'fill-buttons', ...buttons.map((x) => x.btn), noneBtn),
    sync: () => {
      for (const x of buttons) x.sync();
      noneBtn.classList.toggle('active', surf().fill.type === 'none');
    },
  };
  btnRow.sync();

  const detail = dynamicSection(
    () => {
      const f = surf().fill;
      return f.type === 'gradient' ? `gradient:${f.stops.length}` : f.type;
    },
    () => {
      const d = new Section();
      const f = surf().fill;
      if (f.type === 'color') {
        d.add(
          colorRow({
            label: 'Colour',
            get: () => (surf().fill as Extract<Fill, { type: 'color' }>).color,
            set: (hex, merge) => b.edit((l) => { ((l.content as SurfaceContent).fill as Extract<Fill, { type: 'color' }>).color = hex; }, merge ? 'fillcolor' : undefined),
          }),
        );
      } else if (f.type === 'gradient') {
        const g = () => surf().fill as Extract<Fill, { type: 'gradient' }>;
        const editG = (fn: (g: Extract<Fill, { type: 'gradient' }>) => void, key?: string) => b.edit((l) => fn((l.content as SurfaceContent).fill as Extract<Fill, { type: 'gradient' }>), key);
        d.add(
          selectRow({
            label: 'Type',
            options: [{ value: 'linear', label: 'Linear' }, { value: 'radial', label: 'Radial' }],
            get: () => g().kind,
            set: (v) => editG((x) => { x.kind = v as 'linear' | 'radial'; }),
          }),
          numberRow({
            rowLabel: 'Angle',
            tip: 'Direction of a linear gradient, in degrees',
            unit: '°',
            min: 0,
            max: 360,
            step: 1,
            get: () => g().angle,
            set: (v, merge) => editG((x) => { x.angle = v; }, merge ? 'gangle' : undefined),
            disabled: () => g().kind === 'radial',
          }),
        );
        g().stops.forEach((_stop, i) => {
          const stop = () => g().stops[i] as GradientStop;
          const pos = numberRow({
            label: 'Pos',
            unit: '%',
            min: 0,
            max: 100,
            step: 1,
            get: () => Math.round(stop().pos * 100),
            set: (v, merge) => editG((x) => { x.stops[i].pos = v / 100; }, merge ? `gpos${i}` : undefined),
            onEnd: () => editG((x) => x.stops.sort((p, q) => p.pos - q.pos)),
          });
          const col = colorRow({
            label: '',
            get: () => stop().color,
            set: (hex, merge) => editG((x) => { x.stops[i].color = hex; }, merge ? `gcol${i}` : undefined),
          });
          const del = iconButton('x', 'Remove this colour stop', () => editG((x) => x.stops.splice(i, 1)), 12);
          del.disabled = g().stops.length <= 2;
          const row: Row = { el: h('div', 'stop-row', pos.el, col.el, del), sync: () => { pos.sync(); col.sync(); } };
          d.add(row);
        });
        d.addEl(
          h('div', 'row', button('+ Add colour stop', () => editG((x) => {
            const last = x.stops[x.stops.length - 1];
            x.stops.push({ pos: Math.min(1, (last?.pos ?? 0.5) + 0.0), color: last?.color ?? '#ffffff' });
            x.stops.sort((p, q) => p.pos - q.pos);
          }), 'btn small', 'Add another colour to the gradient')),
        );
      } else if (f.type === 'video') {
        const v = () => surf().fill as Extract<Fill, { type: 'video' }>;
        const editV = (fn: (v: Extract<Fill, { type: 'video' }>) => void, key?: string) => b.edit((l) => fn((l.content as SurfaceContent).fill as Extract<Fill, { type: 'video' }>), key);
        d.add(
          toggleRow({ label: 'Loop', tip: 'Start again when the video ends', get: () => v().loop, set: (x) => editV((o) => { o.loop = x; }) }),
          sliderRow({
            label: 'Speed',
            tip: 'Playback rate of the video',
            min: 0.1,
            max: 4,
            step: 0.05,
            decimals: 2,
            def: 1,
            get: () => v().playbackRate,
            set: (x, merge) => editV((o) => { o.playbackRate = x; }, merge ? 'vrate' : undefined),
          }),
        );
      }
      return d;
    },
  );

  s.addEl(sectionLabel('SURFACE'));
  s.add(fieldRow, btnRow, detail);
  return s;
}

// ---------- Shader ----------

export function buildShader(ctx: Ctx, b: Binder, id: string): { section: Section; syncErrors(): void } {
  const s = new Section();
  const sh = () => b.content<ShaderContent>();
  const setShader = (src: string, file: string, builtin: string | null) => b.edit((l) => { l.content = shaderContent(src, file, builtin); });

  const groups = new Map<string, number>();
  for (const x of library.shaders) groups.set(x.category, 1);
  const options = [
    ...library.shaders.map((x) => ({ value: x.id, label: x.name, group: x.category })),
    ...(sh().builtinId === null ? [{ value: '__custom', label: `File: ${sh().fileName || 'custom'}` }] : []),
    { value: '__load', label: 'Load .fs file...' },
  ];
  const pick = selectRow({
    label: 'Shader',
    tip: 'The animated visual shown on this layer',
    options,
    get: () => sh().builtinId ?? '__custom',
    set: (v) => {
      if (v === '__load') {
        void pickFile('.fs,.frag,.txt,text/plain').then(async (file) => {
          if (file) setShader(await file.text(), file.name, null);
          else pick.sync();
        });
        return;
      }
      const lib = library.byId(v);
      if (lib) setShader(lib.source, lib.fileName, lib.id);
      else pick.sync();
    },
  });

  const errBox = h('pre', 'error-box');
  errBox.hidden = true;
  const syncErrors = () => {
    const parts: string[] = [];
    const st = ctx.renderer.status(id);
    if (st.state === 'error') parts.push(st.message);
    const out = ctx.link.status.info?.errors[id];
    if (out) parts.push(`Output window: ${out}`);
    errBox.hidden = parts.length === 0;
    errBox.textContent = parts.join('\n\n');
  };

  const controls = buildShaderControls(sh().source, {
    content: sh,
    setValue: (name, v, merge) => b.edit((l) => { (l.content as ShaderContent).values[name] = v; }, merge ? `sv:${name}` : undefined),
    setAllValues: (vals) => b.edit((l) => { Object.assign((l.content as ShaderContent).values, vals); }),
  });

  s.addEl(sectionLabel('SHADER'));
  s.add(pick);
  s.addEl(errBox);
  if (controls.count > 0) {
    s.addEl(sectionLabel('SHADER PROPERTIES', iconButton('reset', 'Reset every shader setting to its default', () => controls.resetAll(), 13)));
    s.add(controls.section);
  }
  s.addEl(sectionLabel('PLAYBACK'));
  s.add(
    sliderRow({
      label: 'Speed',
      tip: 'How fast this shader runs. 1 is normal, 0 is frozen.',
      min: 0,
      max: 4,
      step: 0.05,
      decimals: 2,
      def: 1,
      get: () => sh().speed,
      set: (v, merge) => b.edit((l) => { (l.content as ShaderContent).speed = v; }, merge ? 'speed' : undefined),
    }),
    toggleRow({ label: 'Pause', tip: 'Freeze this shader only', get: () => sh().paused, set: (v) => b.edit((l) => { (l.content as ShaderContent).paused = v; }) }),
    numberRow({
      rowLabel: 'Time offset',
      tip: 'Start this shader this many seconds into its animation',
      unit: 's',
      step: 0.1,
      get: () => sh().timeOffset,
      set: (v, merge) => b.edit((l) => { (l.content as ShaderContent).timeOffset = v; }, merge ? 'toff' : undefined),
    }),
  );
  syncErrors();
  return { section: s, syncErrors };
}

// ---------- Text ----------

export const FONTS = [
  { value: 'system-ui', label: 'System' },
  { value: 'Georgia, serif', label: 'Serif' },
  { value: 'ui-monospace, Menlo, monospace', label: 'Mono' },
  { value: 'Arial, sans-serif', label: 'Arial' },
  { value: 'Helvetica, Arial, sans-serif', label: 'Helvetica' },
  { value: 'Georgia', label: 'Georgia' },
  { value: '"Times New Roman", serif', label: 'Times New Roman' },
  { value: '"Courier New", monospace', label: 'Courier New' },
  { value: 'Verdana, sans-serif', label: 'Verdana' },
  { value: 'Impact, sans-serif', label: 'Impact' },
];

export function buildText(_ctx: Ctx, b: Binder): Section {
  const s = new Section();
  const t = () => b.content<TextContent>();
  const set = <K extends keyof TextContent>(key: K, merge?: string) => (v: TextContent[K]) => b.edit((l) => { (l.content as TextContent)[key] = v; }, merge);
  const area = h('textarea', 'textarea');
  area.rows = 3;
  area.spellcheck = false;
  area.title = 'The words shown on this layer';
  area.addEventListener('input', () => b.edit((l) => { (l.content as TextContent).text = area.value; }, 'text'));
  area.addEventListener('blur', () => hooks.endMerge());
  const areaRow: Row = { el: area, sync: () => { if (area.value !== t().text) area.value = t().text; } };
  areaRow.sync();

  const nf = (label: string, key: 'fontSize' | 'lineHeight' | 'letterSpacing', o: { min: number; max: number; step: number; unit?: string; tip: string }) =>
    numberRow({ label, title: o.tip, min: o.min, max: o.max, step: o.step, unit: o.unit, get: () => t()[key], set: b.num(`text-${key}`, (l, v) => { (l.content as TextContent)[key] = v; }) });

  s.addEl(sectionLabel('TEXT'));
  s.add(
    areaRow,
    selectRow({ label: 'Font', options: FONTS, get: () => t().fontFamily, set: (v) => set('fontFamily')(v) }),
    pair(
      selectRowBare([100, 200, 300, 400, 500, 600, 700, 800, 900].map((w) => ({ value: String(w), label: weightName(w) })), () => String(t().weight), (v) => set('weight')(parseInt(v, 10))),
      toggleButton('italic', 'Italic', () => t().italic, (v) => set('italic')(v)),
    ),
    pair(nf('Size', 'fontSize', { min: 4, max: 1000, step: 1, unit: 'px', tip: 'Text size in pixels at content resolution' }), nf('Line', 'lineHeight', { min: 0.5, max: 4, step: 0.05, tip: 'Line height as a multiple of the text size' })),
    pair(
      nf('Space', 'letterSpacing', { min: -50, max: 200, step: 0.5, unit: 'px', tip: 'Extra space between letters' }),
      toggleButton('upperCase', 'ALL CAPS', () => t().uppercase, (v) => set('uppercase')(v)),
    ),
    segmented(
      [
        { value: 'left', icon: 'textLeft', title: 'Align left' },
        { value: 'center', icon: 'textCenter', title: 'Align centre' },
        { value: 'right', icon: 'textRight', title: 'Align right' },
        { value: 'justify', icon: 'textJustify', title: 'Justify' },
      ],
      () => t().align,
      (v) => set('align')(v),
    ),
    colorRow({ label: 'Colour', get: () => t().color, set: (hex, merge) => set('color', merge ? 'tcolor' : undefined)(hex) }),
    toggleRow({
      label: 'Background',
      tip: 'Fill behind the text. Off = transparent.',
      get: () => t().background !== null,
      set: (v) => set('background')(v ? '#000000' : null),
    }),
    dynamicSection(
      () => String(t().background !== null),
      () => {
        const d = new Section();
        if (t().background !== null) {
          d.add(colorRow({ label: 'Back colour', get: () => t().background ?? '#000000', set: (hex, merge) => set('background', merge ? 'tbg' : undefined)(hex) }));
        }
        return d;
      },
    ),
  );
  return s;
}

function weightName(w: number): string {
  return { 100: 'Thin', 200: 'Extra Light', 300: 'Light', 400: 'Regular', 500: 'Medium', 600: 'Semi Bold', 700: 'Bold', 800: 'Extra Bold', 900: 'Black' }[w] ?? String(w);
}

function selectRowBare(options: { value: string; label: string }[], get: () => string, set: (v: string) => void): Row {
  return selectControl(options, get, set);
}

// ---------- Webcam ----------

export function buildWebcam(_ctx: Ctx, b: Binder): Section {
  const s = new Section();
  const w = () => b.content<WebcamContent>();
  const holder = h('div', 'cam-holder');
  let cams: { id: string; label: string }[] = [];
  let sig = '';
  let inner: Row | null = null;
  const sync = () => {
    const next = cams.map((c) => c.id).join('|');
    if (next !== sig || !inner) {
      sig = next;
      inner = selectControl(
        [{ value: '', label: 'Default camera' }, ...cams.map((c, i) => ({ value: c.id, label: c.label || `Camera ${i + 1}` }))],
        () => w().deviceId ?? '',
        (v) => b.edit((l) => { (l.content as WebcamContent).deviceId = v || null; }),
      );
      holder.replaceChildren(inner.el);
    }
    inner.sync();
  };
  const load = async () => {
    try {
      const devices = await navigator.mediaDevices.enumerateDevices();
      cams = devices.filter((d) => d.kind === 'videoinput').map((d) => ({ id: d.deviceId, label: d.label }));
    } catch {
      cams = [];
    }
    sync();
  };
  void load();
  navigator.mediaDevices?.addEventListener?.('devicechange', () => void load());
  s.addEl(sectionLabel('WEBCAM'));
  s.add(
    { el: labelled('Camera', holder, 'Which camera to use. Names appear after the browser has been given camera access.'), sync },
    toggleRow({ label: 'Mirror', tip: 'Flip the picture left to right, like a mirror', get: () => w().mirror, set: (v) => b.edit((l) => { (l.content as WebcamContent).mirror = v; }) }),
  );
  return s;
}

// ---------- Mask ----------

export function buildMask(ctx: Ctx, id: string): Section {
  const s = new Section();
  const m = () => findLayer(ctx.store.state, id) as MaskLayer;
  const edit = (fn: (m: MaskLayer) => void, key?: string) => updateLayer<MaskLayer>(ctx, id, fn, key);
  const targets = activeScene(ctx.store.state).layers.filter((l) => l.kind === 'content');
  s.addEl(sectionLabel('MASK'));
  s.add(
    selectRow({
      label: 'Applies to',
      tip: 'Which layers this mask affects',
      options: [{ value: 'below', label: 'Layers below' }, ...targets.map((l) => ({ value: l.id, label: l.name }))],
      get: () => m().target,
      set: (v) => edit((x) => { x.target = v; }),
    }),
    selectRow({
      label: 'Mode',
      tip: 'Hide inside cuts a hole. Show only inside hides everything outside the shape.',
      options: [{ value: 'hide', label: 'Hide inside' }, { value: 'show', label: 'Show only inside' }],
      get: () => (m().invert ? 'show' : 'hide'),
      set: (v) => edit((x) => { x.invert = v === 'show'; }),
    }),
    numberRow({
      rowLabel: 'Feather',
      tip: 'Soft edge width in pixels',
      unit: 'px',
      min: 0,
      max: 500,
      step: 1,
      get: () => m().feather,
      set: (v, merge) => edit((x) => { x.feather = v; }, merge ? 'mfeather' : undefined),
    }),
    sliderRow({
      label: 'Opacity',
      tip: 'How strongly the mask applies',
      min: 0,
      max: 100,
      step: 1,
      unit: '%',
      def: 100,
      get: () => Math.round(m().opacity * 100),
      set: (v, merge) => edit((x) => { x.opacity = v / 100; }, merge ? 'mop' : undefined),
    }),
    colorRow({
      label: 'Editor colour',
      tip: 'The colour the mask is drawn with in this editor. It never shows on the projector.',
      get: () => m().color,
      set: (hex, merge) => edit((x) => { x.color = hex; }, merge ? 'mcol' : undefined),
    }),
  );
  return s;
}
