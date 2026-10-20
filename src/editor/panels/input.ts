// Input panel: the selected layer's raw content, with the crop rectangle that
// decides which part of it is mapped onto the shape.

import { bounds } from '../../geometry/homography';
import type { ContentLayer, CropRect, Vec2 } from '../../state/types';
import type { Ctx } from '../ctx';
import { clamp, h } from '../dom';
import { selectedLayer } from '../layerOps';
import { PANEL_LABELS } from '../uiState';
import { keys, Viewport } from '../viewport';
import { hooks } from '../widgets/numberField';
import { panelShell } from '../widgets/panel';

const HANDLE_HIT = 9;
const MIN_CROP = 0.02;
const BLUE = '#3B82F6';

type Drag = { kind: 'corner'; idx: number; orig: CropRect } | { kind: 'move'; orig: CropRect; start: Vec2 };

export function createInputPanel(ctx: Ctx) {
  const { store, ui, renderer } = ctx;
  const shell = panelShell(PANEL_LABELS.input.title, PANEL_LABELS.input.key, 'input-panel');
  const vp = new Viewport();
  const zoomBtn = h('button', 'zoom-readout', '100%');
  zoomBtn.type = 'button';
  zoomBtn.title = 'Zoom level. Click to fit. Scroll to zoom, hold Space and drag to pan.';
  zoomBtn.addEventListener('click', () => vp.fit());
  shell.actions.append(zoomBtn);
  const hint = h('div', 'vp-hint', 'Select a layer to see its input');
  shell.body.append(vp.el);
  vp.el.append(hint);

  let dirty = true;
  const invalidate = () => {
    dirty = true;
  };
  vp.onChange = () => {
    zoomBtn.textContent = vp.zoomLabel;
    invalidate();
  };
  store.subscribe(invalidate);
  ui.subscribe(invalidate);

  const currentLayer = (): ContentLayer | null => {
    const l = selectedLayer(ctx);
    return l && l.kind === 'content' ? l : null;
  };

  function contentDims(l: ContentLayer): [number, number] {
    const native = renderer.contentSize(l.id);
    if (native) return [native.width, native.height];
    const b = bounds(l.corners);
    const aspect = b.w > 0 && b.h > 0 ? b.w / b.h : 1;
    return aspect >= 1 ? [1000, 1000 / aspect] : [1000 * aspect, 1000];
  }

  const cropRectScreen = (c: CropRect) => {
    const a = vp.toScreen([c.x * vp.contentW, c.y * vp.contentH]);
    const b = vp.toScreen([(c.x + c.w) * vp.contentW, (c.y + c.h) * vp.contentH]);
    return { x: a[0], y: a[1], w: b[0] - a[0], h: b[1] - a[1] };
  };
  const cornerScreen = (c: CropRect): Vec2[] => {
    const r = cropRectScreen(c);
    return [[r.x, r.y], [r.x + r.w, r.y], [r.x + r.w, r.y + r.h], [r.x, r.y + r.h]];
  };
  const toNorm = (p: Vec2): Vec2 => {
    const c = vp.toContent(p);
    return [c[0] / vp.contentW, c[1] / vp.contentH];
  };

  let drag: Drag | null = null;

  const setCrop = (fn: (c: CropRect) => void) =>
    store.update(
      (p) => {
        const l = selectedLayerIn(p);
        if (l) fn(l.crop);
      },
      { history: 'merge', key: `crop:${ui.state.selectedId}` },
    );
  const selectedLayerIn = (p: typeof store.state): ContentLayer | null => {
    for (const s of p.scenes) {
      const l = s.layers.find((x) => x.id === ui.state.selectedId);
      if (l && l.kind === 'content') return l;
    }
    return null;
  };

  function hitCorner(l: ContentLayer, p: Vec2): number {
    let best = -1;
    let bestD = HANDLE_HIT;
    cornerScreen(l.crop).forEach((q, i) => {
      const d = Math.hypot(q[0] - p[0], q[1] - p[1]);
      if (d <= bestD) {
        bestD = d;
        best = i;
      }
    });
    return best;
  }
  const insideCrop = (l: ContentLayer, p: Vec2) => {
    const r = cropRectScreen(l.crop);
    return p[0] >= r.x && p[0] <= r.x + r.w && p[1] >= r.y && p[1] <= r.y + r.h;
  };

  vp.el.addEventListener('pointerdown', (e) => {
    if (vp.beginPan(e)) return;
    const l = currentLayer();
    if (e.button !== 0 || !l || l.locked) return;
    const p = vp.pointer(e);
    const idx = hitCorner(l, p);
    if (idx >= 0) drag = { kind: 'corner', idx, orig: { ...l.crop } };
    else if (insideCrop(l, p)) drag = { kind: 'move', orig: { ...l.crop }, start: toNorm(p) };
    if (drag) vp.el.setPointerCapture(e.pointerId);
  });

  vp.el.addEventListener('pointermove', (e) => {
    const l = currentLayer();
    if (!l) return;
    const p = vp.pointer(e);
    if (!drag) {
      vp.overlay.style.cursor = keys.space ? 'grab' : hitCorner(l, p) >= 0 ? 'crosshair' : insideCrop(l, p) ? 'move' : '';
      return;
    }
    const n = toNorm(p);
    const d = drag;
    if (d.kind === 'move') {
      const dx = n[0] - d.start[0];
      const dy = n[1] - d.start[1];
      setCrop((c) => {
        c.x = clamp(d.orig.x + dx, 0, 1 - d.orig.w);
        c.y = clamp(d.orig.y + dy, 0, 1 - d.orig.h);
      });
    } else {
      // The corner opposite the dragged one stays put.
      const o = d.orig;
      const left = d.idx === 0 || d.idx === 3;
      const top = d.idx === 0 || d.idx === 1;
      const fixedX = left ? o.x + o.w : o.x;
      const fixedY = top ? o.y + o.h : o.y;
      const x = clamp(n[0], 0, 1);
      const y = clamp(n[1], 0, 1);
      const nx = left ? Math.min(x, fixedX - MIN_CROP) : Math.max(x, fixedX + MIN_CROP);
      const ny = top ? Math.min(y, fixedY - MIN_CROP) : Math.max(y, fixedY + MIN_CROP);
      setCrop((c) => {
        c.x = Math.min(nx, fixedX);
        c.y = Math.min(ny, fixedY);
        c.w = Math.abs(nx - fixedX);
        c.h = Math.abs(ny - fixedY);
      });
    }
  });
  const end = () => {
    if (drag) hooks.endMerge();
    drag = null;
  };
  vp.el.addEventListener('pointerup', end);
  vp.el.addEventListener('pointercancel', end);
  vp.el.addEventListener('dblclick', (e) => {
    const l = currentLayer();
    if (!l || !insideCrop(l, vp.pointer(e))) vp.fit();
  });

  function drawOverlay(l: ContentLayer | null): void {
    const c = vp.overlay.getContext('2d');
    if (!c) return;
    c.setTransform(vp.dpr, 0, 0, vp.dpr, 0, 0);
    c.clearRect(0, 0, vp.width, vp.height);
    const r = vp.rect();
    if (!l) {
      c.fillStyle = '#1C1C1F';
      c.fillRect(r.x, r.y, r.w, r.h);
      return;
    }
    c.strokeStyle = '#2A2A2E';
    c.strokeRect(r.x - 0.5, r.y - 0.5, r.w + 1, r.h + 1);
    const cr = cropRectScreen(l.crop);
    // Dim everything outside the crop.
    c.fillStyle = 'rgba(0,0,0,0.55)';
    c.beginPath();
    c.rect(r.x, r.y, r.w, r.h);
    c.rect(cr.x, cr.y, cr.w, cr.h);
    c.fill('evenodd');
    c.strokeStyle = BLUE;
    c.lineWidth = 1.5;
    c.strokeRect(cr.x, cr.y, cr.w, cr.h);
    for (const [x, y] of cornerScreen(l.crop)) {
      c.fillStyle = BLUE;
      c.strokeStyle = '#FFFFFF';
      c.lineWidth = 1;
      c.fillRect(x - 4, y - 4, 8, 8);
      c.strokeRect(x - 4, y - 4, 8, 8);
    }
  }

  function render(): void {
    if (!ui.state.panels.input) return;
    const l = currentLayer();
    hint.hidden = !!l;
    vp.display.style.display = l ? '' : 'none';
    if (l) {
      const [w, hh] = contentDims(l);
      vp.setContentSize(w, hh);
    } else {
      vp.setContentSize(store.state.canvas.width, store.state.canvas.height);
    }
    if (l) {
      vp.layoutDisplay();
      const d = vp.prepareDisplay(1280);
      if (d && vp.width > 4) {
        renderer.renderContent(l.id, d.w, d.h);
        renderer.copyTo(d.ctx);
      }
    }
    if (dirty) {
      dirty = false;
      drawOverlay(l);
    }
  }
  return { el: shell.el, render, vp };
}
