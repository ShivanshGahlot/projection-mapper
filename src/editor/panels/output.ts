// Output panel: live preview of what the projector shows, with on-canvas editing
// (corner-pin handles, move, mask points), X-ray, output-size crop banner and stats.

import { containsPoint } from '../../geometry/homography';
import { activeScene } from '../../state/defaults';
import type { Layer, Vec2 } from '../../state/types';
import type { Ctx } from '../ctx';
import { h } from '../dom';
import { icon } from '../icons';
import { layerPoints, translateLayer } from '../geometryOps';
import { findLayer, selectedLayer } from '../layerOps';
import { PANEL_LABELS } from '../uiState';
import { keys, Viewport } from '../viewport';
import { hooks } from '../widgets/numberField';
import { openPopover } from '../widgets/overlays';
import { panelShell } from '../widgets/panel';
import { iconButton } from '../widgets/rows';
import { buildOutputSettings } from './outputSettings';

const HANDLE_HIT = 9;
const BLUE = '#3B82F6';

type Drag =
  | { kind: 'edge'; id: string; idx: number; start: Vec2; orig: Vec2[] }
  | { kind: 'point'; id: string; idx: number; offset: Vec2; orig: Vec2 }
  | { kind: 'move'; id: string; start: Vec2; orig: Vec2[]; active: boolean; startScreen: Vec2 };

export function createOutputPanel(ctx: Ctx) {
  const { store, ui, renderer, link } = ctx;
  const shell = panelShell(PANEL_LABELS.output.title, PANEL_LABELS.output.key, 'output-panel');
  const vp = new Viewport();

  const xrayBtn = iconButton('xray', 'X-ray: show outlines of every layer, even hidden ones', () => ui.set({ xray: !ui.state.xray }), 15);
  const gearBtn = iconButton('gear', 'Output settings: canvas size, brightness, test pattern...', () => openSettings(), 15);
  const zoomBtn = h('button', 'zoom-readout', '100%');
  zoomBtn.type = 'button';
  zoomBtn.title = 'Zoom level. Click to fit the picture to the panel. Scroll to zoom, hold Space and drag to pan.';
  zoomBtn.addEventListener('click', () => vp.fit());
  shell.actions.append(xrayBtn, gearBtn, zoomBtn);

  const banner = h('div', 'banner');
  const bannerText = h('span');
  const cropBtn = h('button', 'btn small', 'Crop to Fit');
  cropBtn.type = 'button';
  cropBtn.title = 'Make the canvas the same size as the output window. Shapes keep their pixel positions, so the picture is cropped.';
  banner.append(icon('monitor', 13), bannerText, cropBtn);
  banner.hidden = true;
  const stats = h('pre', 'stats');
  stats.hidden = true;
  shell.body.append(banner, vp.el);
  vp.el.append(stats);

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
  link.onStatus(invalidate);

  let settingsOpen = false;
  function openSettings(): void {
    if (settingsOpen) return;
    settingsOpen = true;
    const section = buildOutputSettings(ctx);
    const unsub = store.subscribe(() => section.sync());
    const unsubUi = ui.subscribe(() => section.sync());
    openPopover(gearBtn, section.el, () => {
      settingsOpen = false;
      unsub();
      unsubUi();
    });
  }

  function outputSize(): { width: number; height: number } | null {
    const st = link.status;
    return st.state === 'connected' && st.info?.fullscreen ? { width: st.info.width, height: st.info.height } : null;
  }

  cropBtn.addEventListener('click', () => {
    const o = outputSize();
    if (o) store.update((p) => { p.canvas = { width: o.width, height: o.height }; });
  });

  // ---- hit testing ----
  function handleAt(layer: Layer, p: Vec2): number {
    const pts = layerPoints(layer);
    let best = -1;
    let bestD = HANDLE_HIT;
    pts.forEach((pt, i) => {
      const s = vp.toScreen(pt);
      const d = Math.hypot(s[0] - p[0], s[1] - p[1]);
      if (d <= bestD) {
        bestD = d;
        best = i;
      }
    });
    return best;
  }

  function sideAt(layer: Layer, p: Vec2): number {
    if (layer.kind !== 'content') return -1;
    for (let i = 0; i < 4; i++) {
      const a = layer.corners[i], b = layer.corners[(i + 1) % 4];
      const q = vp.toScreen([(a[0] + b[0]) / 2, (a[1] + b[1]) / 2]);
      if (Math.hypot(q[0] - p[0], q[1] - p[1]) <= HANDLE_HIT) return i;
    }
    return -1;
  }

  function layerAt(c: Vec2): Layer | null {
    const layers = activeScene(store.state).layers;
    for (let i = layers.length - 1; i >= 0; i--) {
      const l = layers[i];
      if (l.kind === 'content' && l.visible && !l.locked && containsPoint(l.corners, c)) return l;
    }
    for (let i = layers.length - 1; i >= 0; i--) {
      const l = layers[i];
      if (l.kind === 'mask' && l.visible && !l.locked && containsPoint(l.points, c)) return l;
    }
    return null;
  }

  function edgeAt(l: Layer, p: Vec2): { index: number; point: Vec2 } | null {
    const pts = layerPoints(l);
    for (let i = 0; i < pts.length; i++) {
      const a = vp.toScreen(pts[i]);
      const b = vp.toScreen(pts[(i + 1) % pts.length]);
      const dx = b[0] - a[0];
      const dy = b[1] - a[1];
      const len2 = dx * dx + dy * dy || 1;
      const t = Math.max(0, Math.min(1, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dy) / len2));
      if (Math.hypot(a[0] + t * dx - p[0], a[1] + t * dy - p[1]) <= 7) return { index: i, point: vp.toContent([a[0] + t * dx, a[1] + t * dy]) };
    }
    return null;
  }

  // ---- pointer interaction ----
  let drag: Drag | null = null;

  vp.el.addEventListener('pointerdown', (e) => {
    if (vp.beginPan(e)) return;
    if (e.button !== 0) return;
    const p = vp.pointer(e);
    const c = vp.toContent(p);
    const sel = selectedLayer(ctx);
    if (sel) {
      const idx = handleAt(sel, p);
      if (idx >= 0) {
        if (sel.kind === 'mask' && e.altKey) {
          if (sel.points.length > 3) {
            store.update(() => {
              (findLayer(store.state, sel.id) as typeof sel).points.splice(idx, 1);
            });
            ui.set({ selectedCorner: null });
          }
          return;
        }
        ui.set({ selectedCorner: idx });
        if (!sel.locked) {
          const orig = layerPoints(sel)[idx];
          drag = { kind: 'point', id: sel.id, idx, offset: [orig[0] - c[0], orig[1] - c[1]], orig: [...orig] };
          vp.el.setPointerCapture(e.pointerId);
        }
        return;
      }
    }
    if (sel && !sel.locked) {
      const idx = sideAt(sel, p);
      if (idx >= 0) {
        ui.set({ selectedCorner: null });
        drag = { kind: 'edge', id: sel.id, idx, start: c, orig: layerPoints(sel).map((q) => [...q] as Vec2) };
        vp.el.setPointerCapture(e.pointerId);
        return;
      }
    }
    const hit = layerAt(c);
    if (!hit) {
      ui.select(null);
      return;
    }
    ui.set({ selectedId: hit.id, selectedCorner: null });
    drag = { kind: 'move', id: hit.id, start: c, orig: layerPoints(hit).map((q) => [q[0], q[1]] as Vec2), active: false, startScreen: p };
    vp.el.setPointerCapture(e.pointerId);
  });

  vp.el.addEventListener('pointermove', (e) => {
    const p = vp.pointer(e);
    const c = vp.toContent(p);
    if (!drag) {
      updateHover(p, c);
      return;
    }
    const d = drag;
    if (d.kind === 'edge') {
      let dx = c[0] - d.start[0], dy = c[1] - d.start[1];
      if (e.shiftKey) { if (Math.abs(dx) >= Math.abs(dy)) dy = 0; else dx = 0; }
      store.update((pr) => {
        const l = findLayer(pr, d.id);
        if (!l || l.locked) return;
        for (const i of [d.idx, (d.idx + 1) % 4]) {
          layerPoints(l)[i] = [Math.round((d.orig[i][0] + dx) * 10) / 10, Math.round((d.orig[i][1] + dy) * 10) / 10];
        }
      }, { history: 'merge', key: `edge:${d.id}:${d.idx}` });
    } else if (d.kind === 'point') {
      let x = c[0] + d.offset[0];
      let y = c[1] + d.offset[1];
      if (e.shiftKey) {
        // Snap the drag direction to 15 degree steps.
        const dx = x - d.orig[0];
        const dy = y - d.orig[1];
        const len = Math.hypot(dx, dy);
        const ang = Math.round(Math.atan2(dy, dx) / (Math.PI / 12)) * (Math.PI / 12);
        x = d.orig[0] + Math.cos(ang) * len;
        y = d.orig[1] + Math.sin(ang) * len;
      }
      store.update(
        (pr) => {
          const l = findLayer(pr, d.id);
          if (l) layerPoints(l)[d.idx] = [Math.round(x * 10) / 10, Math.round(y * 10) / 10];
        },
        { history: 'merge', key: `drag:${d.id}:${d.idx}` },
      );
    } else {
      if (!d.active) {
        if (Math.hypot(p[0] - d.startScreen[0], p[1] - d.startScreen[1]) < 3) return;
        d.active = true;
      }
      const dx = c[0] - d.start[0];
      const dy = c[1] - d.start[1];
      store.update(
        (pr) => {
          const l = findLayer(pr, d.id);
          if (!l || l.locked) return;
          layerPoints(l).forEach((_pt, i) => {
            layerPoints(l)[i] = [Math.round((d.orig[i][0] + dx) * 10) / 10, Math.round((d.orig[i][1] + dy) * 10) / 10];
          });
        },
        { history: 'merge', key: `move:${d.id}` },
      );
    }
  });

  const endDrag = () => {
    if (drag) hooks.endMerge();
    drag = null;
  };
  vp.el.addEventListener('pointerup', endDrag);
  vp.el.addEventListener('pointercancel', endDrag);

  vp.el.addEventListener('dblclick', (e) => {
    const p = vp.pointer(e);
    const sel = selectedLayer(ctx);
    if (sel && sel.kind === 'mask') {
      const edge = edgeAt(sel, p);
      if (edge && handleAt(sel, p) < 0) {
        store.update((pr) => {
          const m = findLayer(pr, sel.id);
          if (m && m.kind === 'mask') m.points.splice(edge.index + 1, 0, [Math.round(edge.point[0]), Math.round(edge.point[1])]);
        });
        ui.set({ selectedCorner: edge.index + 1 });
        return;
      }
    }
    const r = vp.rect();
    const inside = p[0] >= r.x && p[0] <= r.x + r.w && p[1] >= r.y && p[1] <= r.y + r.h;
    if (!inside) vp.fit();
  });

  vp.el.addEventListener('pointerleave', () => {
    if (!drag && ui.state.hoverId) ui.set({ hoverId: null });
  });

  function updateHover(p: Vec2, c: Vec2): void {
    const sel = selectedLayer(ctx);
    let cursor = '';
    let hover: string | null = null;
    if (keys.space) cursor = 'grab';
    else if (sel && !sel.locked && sideAt(sel, p) >= 0) cursor = 'move';
    else if (sel && handleAt(sel, p) >= 0) cursor = sel.kind === 'mask' ? 'pointer' : 'crosshair';
    else {
      const hit = layerAt(c);
      if (hit) {
        hover = hit.id;
        cursor = hit.id === sel?.id ? 'move' : 'pointer';
      }
    }
    vp.overlay.style.cursor = cursor;
    if (hover !== ui.state.hoverId) ui.set({ hoverId: hover });
  }

  // ---- keyboard helpers used by keyboard.ts ----
  function nudge(dx: number, dy: number): void {
    const sel = selectedLayer(ctx);
    if (!sel || sel.locked) return;
    const corner = ui.state.selectedCorner;
    store.update(
      (p) => {
        const l = findLayer(p, sel.id);
        if (!l) return;
        if (corner !== null && layerPoints(l)[corner]) {
          const q = layerPoints(l)[corner];
          layerPoints(l)[corner] = [q[0] + dx, q[1] + dy];
        } else translateLayer(l, dx, dy);
      },
      { history: 'merge', key: `nudge:${sel.id}:${corner}` },
    );
  }

  // ---- drawing ----
  function drawOverlay(): void {
    const c = vp.overlay.getContext('2d');
    if (!c) return;
    const dpr = vp.dpr;
    c.setTransform(dpr, 0, 0, dpr, 0, 0);
    c.clearRect(0, 0, vp.width, vp.height);
    const r = vp.rect();
    c.strokeStyle = '#2A2A2E';
    c.lineWidth = 1;
    c.strokeRect(r.x - 0.5, r.y - 0.5, r.w + 1, r.h + 1);

    const out = outputSize();
    const cv = store.state.canvas;
    if (out && (out.width !== cv.width || out.height !== cv.height)) {
      const a = vp.toScreen([0, 0]);
      const b = vp.toScreen([out.width, out.height]);
      c.save();
      c.strokeStyle = '#EAB308';
      c.setLineDash([6, 4]);
      c.lineWidth = 1.5;
      c.strokeRect(a[0], a[1], b[0] - a[0], b[1] - a[1]);
      c.setLineDash([]);
      c.fillStyle = '#EAB308';
      c.font = '11px system-ui, sans-serif';
      c.fillText(`${out.width} x ${out.height} Crop`, a[0] + 6, a[1] + 14);
      c.restore();
    }

    const scene = activeScene(store.state);
    const selId = ui.state.selectedId;
    const path = (l: Layer, closed = true) => {
      const pts = layerPoints(l).map((q) => vp.toScreen(q));
      c.beginPath();
      pts.forEach((q, i) => (i ? c.lineTo(q[0], q[1]) : c.moveTo(q[0], q[1])));
      if (closed) c.closePath();
    };
    for (const l of scene.layers) {
      const isSel = l.id === selId;
      const isHover = l.id === ui.state.hoverId;
      if (!isSel && !isHover && !ui.state.xray) continue;
      path(l);
      if (l.kind === 'mask') {
        c.globalAlpha = isSel ? 0.3 : 0.14;
        c.fillStyle = l.color;
        c.fill();
        c.globalAlpha = 1;
        c.strokeStyle = l.color;
      } else c.strokeStyle = isSel ? BLUE : 'rgba(255,255,255,0.55)';
      c.lineWidth = isSel ? 1.5 : 1;
      c.globalAlpha = isSel ? 1 : isHover ? 0.9 : 0.4;
      c.setLineDash(l.visible ? [] : [4, 3]);
      c.stroke();
      c.setLineDash([]);
      c.globalAlpha = 1;
    }

    const sel = selectedLayer(ctx);
    if (sel) {
      const pts = layerPoints(sel);
      if (sel.kind === 'content') {
        for (let i = 0; i < 4; i++) {
          const a = pts[i], b = pts[(i + 1) % 4];
          const [x, y] = vp.toScreen([(a[0] + b[0]) / 2, (a[1] + b[1]) / 2]);
          c.fillStyle = sel.locked ? '#777777' : BLUE; c.strokeStyle = '#FFFFFF'; c.lineWidth = 1;
          c.beginPath(); c.arc(x, y, 4, 0, Math.PI * 2); c.fill(); c.stroke();
        }
      }
      pts.forEach((q, i) => {
        const [x, y] = vp.toScreen(q);
        const active = ui.state.selectedCorner === i;
        const size = active ? 10 : 8;
        c.fillStyle = active ? '#FFFFFF' : BLUE;
        c.strokeStyle = active ? BLUE : '#FFFFFF';
        c.lineWidth = active ? 2 : 1;
        if (sel.kind === 'mask') {
          c.beginPath();
          c.arc(x, y, size / 2 + 0.5, 0, Math.PI * 2);
          c.fill();
          c.stroke();
        } else {
          c.fillRect(x - size / 2, y - size / 2, size, size);
          c.strokeRect(x - size / 2, y - size / 2, size, size);
        }
      });
    }
  }

  let statsAt = 0;
  function updateStats(now: number): void {
    stats.hidden = !ui.state.showStats;
    if (stats.hidden || now - statsAt < 250) return;
    statsAt = now;
    const st = renderer.stats;
    const lines = [`${st.fps.toFixed(0)} fps (editor)`];
    const o = link.status.info;
    if (o) lines.push(`${o.fps.toFixed(0)} fps (output window)`);
    for (const l of activeScene(store.state).layers) {
      const ms = st.layerMs[l.id];
      if (ms !== undefined) lines.push(`${l.name}: ${ms.toFixed(2)} ms`);
    }
    stats.textContent = lines.join('\n');
  }

  function render(now: number): void {
    if (!ui.state.panels.output) return;
    const cv = store.state.canvas;
    vp.setContentSize(cv.width, cv.height);
    vp.layoutDisplay();
    const d = vp.prepareDisplay();
    if (d && vp.width > 4) {
      renderer.renderOutput(d.w, d.h);
      renderer.copyTo(d.ctx);
    }
    xrayBtn.classList.toggle('active', ui.state.xray);
    const out = outputSize();
    const mismatch = !!out && (out.width !== cv.width || out.height !== cv.height);
    banner.hidden = !mismatch;
    if (mismatch && out) bannerText.textContent = `Output window is ${out.width}×${out.height}, canvas is ${cv.width}×${cv.height}`;
    updateStats(now);
    if (dirty) {
      dirty = false;
      drawOverlay();
    }
  }

  return { el: shell.el, render, nudge, vp, openSettings };
}
