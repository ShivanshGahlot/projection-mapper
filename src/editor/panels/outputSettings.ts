// The gear popover in the Output panel: canvas size, brightness, pause, test pattern...

import { setPaused } from '../commands';
import type { Ctx } from '../ctx';
import { h } from '../dom';
import { numberRow, pair, Section, selectRow, sliderRow, toggleRow, colorRow } from '../widgets/rows';

export const CANVAS_PRESETS: { id: string; label: string; w: number; h: number }[] = [
  { id: '720p', label: '720p (1280 x 720)', w: 1280, h: 720 },
  { id: '1080p', label: '1080p (1920 x 1080)', w: 1920, h: 1080 },
  { id: '2k', label: '2K (2560 x 1440)', w: 2560, h: 1440 },
  { id: '4k', label: '4K (3840 x 2160)', w: 3840, h: 2160 },
  { id: 'wuxga', label: 'WUXGA (1920 x 1200)', w: 1920, h: 1200 },
  { id: 'wxga', label: 'WXGA (1280 x 800)', w: 1280, h: 800 },
];

export function buildOutputSettings(ctx: Ctx): Section {
  const { store, ui } = ctx;
  const s = new Section('settings');
  s.addEl(h('div', 'popover-title', 'Output settings'));

  const setCanvas = (w: number | null, hh: number | null) =>
    store.update((p) => {
      if (w !== null) p.canvas.width = Math.round(w);
      if (hh !== null) p.canvas.height = Math.round(hh);
    });

  s.add(
    selectRow({
      label: 'Canvas size',
      tip: 'The size of the picture your projector or screen shows, in pixels. It should match the projector resolution.',
      options: [{ value: 'custom', label: 'Custom' }, ...CANVAS_PRESETS.map((p) => ({ value: p.id, label: p.label }))],
      get: () => CANVAS_PRESETS.find((p) => p.w === store.state.canvas.width && p.h === store.state.canvas.height)?.id ?? 'custom',
      set: (v) => {
        const p = CANVAS_PRESETS.find((x) => x.id === v);
        if (p) setCanvas(p.w, p.h);
      },
    }),
    pair(
      numberRow({ label: 'W', title: 'Canvas width in pixels', min: 64, max: 16384, step: 1, get: () => store.state.canvas.width, set: (v) => setCanvas(v, null) }),
      numberRow({ label: 'H', title: 'Canvas height in pixels', min: 64, max: 16384, step: 1, get: () => store.state.canvas.height, set: (v) => setCanvas(null, v) }),
    ),
    sliderRow({
      label: 'Brightness',
      tip: 'Master brightness for the whole output, like a dimmer.',
      min: 0,
      max: 100,
      step: 1,
      unit: '%',
      get: () => Math.round(store.state.output.brightness * 100),
      set: (v, merge) => store.update((p) => { p.output.brightness = v / 100; }, merge ? { history: 'merge', key: 'brightness' } : {}),
    }),
    toggleRow({
      label: 'Pause playback',
      tip: 'Freezes all animated shaders and videos (same as the Pause button on the top bar).',
      get: () => store.state.playback.paused,
      set: (v) => setPaused(ctx, v),
    }),
    toggleRow({
      label: 'Test pattern',
      tip: 'Shows a grid over the whole output so you can line up the projector.',
      get: () => store.state.output.testPattern,
      set: (v) => store.update((p) => { p.output.testPattern = v; }),
    }),
    colorRow({
      label: 'Background',
      tip: 'The colour behind all layers.',
      get: () => store.state.output.background,
      set: (hex, merge) => store.update((p) => { p.output.background = hex; }, merge ? { history: 'merge', key: 'bg' } : {}),
    }),
    toggleRow({
      label: 'Performance stats',
      tip: 'Shows frames per second and how long each layer takes to draw.',
      get: () => ui.state.showStats,
      set: (v) => ui.set({ showStats: v }),
    }),
  );
  return s;
}
