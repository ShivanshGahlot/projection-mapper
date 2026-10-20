// Zoom/pan viewport shared by the Input and Output panels. A display canvas
// shows the rendered picture at the canvas rectangle; a transparent overlay
// canvas on top is used for handles and outlines.

import type { Vec2 } from '../state/types';
import { clamp, h } from './dom';

export const keys = { space: false, spacePanned: false };

const PAD = 14;

export class Viewport {
  readonly el: HTMLElement;
  /** Picture, sized and placed over the canvas rectangle. */
  readonly display: HTMLCanvasElement;
  /** Handles and outlines, covering the whole panel body. */
  readonly overlay: HTMLCanvasElement;
  zoom = 1;
  panX = 0;
  panY = 0;
  /** Size of the thing being shown, in its own units. */
  contentW = 1920;
  contentH = 1080;
  width = 1;
  height = 1;
  dpr = window.devicePixelRatio || 1;
  onChange: () => void = () => {};
  private panning: { x: number; y: number; px: number; py: number } | null = null;

  constructor() {
    this.display = h('canvas', 'vp-display');
    this.overlay = h('canvas', 'vp-overlay');
    this.el = h('div', 'vp', this.display, this.overlay);
    new ResizeObserver(() => this.resize()).observe(this.el);
    this.el.addEventListener('wheel', (e) => this.onWheel(e), { passive: false });
  }

  resize(): void {
    const r = this.el.getBoundingClientRect();
    this.width = Math.max(1, r.width);
    this.height = Math.max(1, r.height);
    this.dpr = window.devicePixelRatio || 1;
    this.overlay.width = Math.round(this.width * this.dpr);
    this.overlay.height = Math.round(this.height * this.dpr);
    this.onChange();
  }

  setContentSize(w: number, h: number): void {
    if (w === this.contentW && h === this.contentH) return;
    this.contentW = w;
    this.contentH = h;
    this.onChange();
  }

  /** Scale that makes the content fit the panel at zoom 100%. */
  get fitScale(): number {
    return Math.min((this.width - PAD * 2) / this.contentW, (this.height - PAD * 2) / this.contentH);
  }

  /** Content pixels -> screen px. */
  get scale(): number {
    return this.fitScale * this.zoom;
  }

  rect(): { x: number; y: number; w: number; h: number } {
    const s = this.scale;
    const w = this.contentW * s;
    const hh = this.contentH * s;
    return { x: (this.width - w) / 2 + this.panX, y: (this.height - hh) / 2 + this.panY, w, h: hh };
  }

  toScreen(p: Vec2): Vec2 {
    const r = this.rect();
    const s = this.scale;
    return [r.x + p[0] * s, r.y + p[1] * s];
  }

  toContent(p: Vec2): Vec2 {
    const r = this.rect();
    const s = this.scale;
    return [(p[0] - r.x) / s, (p[1] - r.y) / s];
  }

  /** Pointer position relative to the panel body. */
  pointer(e: { clientX: number; clientY: number }): Vec2 {
    const r = this.el.getBoundingClientRect();
    return [e.clientX - r.left, e.clientY - r.top];
  }

  fit(): void {
    this.zoom = 1;
    this.panX = 0;
    this.panY = 0;
    this.onChange();
  }

  /** Places the display canvas over the content rectangle. */
  layoutDisplay(): void {
    const r = this.rect();
    const st = this.display.style;
    st.left = `${r.x}px`;
    st.top = `${r.y}px`;
    st.width = `${r.w}px`;
    st.height = `${r.h}px`;
  }

  private onWheel(e: WheelEvent): void {
    e.preventDefault();
    const p = this.pointer(e);
    const before = this.toContent(p);
    const factor = Math.exp(-e.deltaY * (e.ctrlKey ? 0.01 : 0.0015));
    this.zoom = clamp(this.zoom * factor, 0.1, 32);
    // Keep the point under the cursor fixed.
    const s = this.scale;
    this.panX = p[0] - (this.width - this.contentW * s) / 2 - before[0] * s;
    this.panY = p[1] - (this.height - this.contentH * s) / 2 - before[1] * s;
    this.onChange();
  }

  /** Starts a pan if this press should pan (middle button or Space). Returns true when it did. */
  beginPan(e: PointerEvent): boolean {
    if (!(e.button === 1 || (e.button === 0 && keys.space))) return false;
    e.preventDefault();
    keys.spacePanned = true;
    this.panning = { x: e.clientX, y: e.clientY, px: this.panX, py: this.panY };
    this.el.setPointerCapture(e.pointerId);
    this.el.classList.add('panning');
    const move = (ev: PointerEvent) => {
      if (!this.panning) return;
      this.panX = this.panning.px + ev.clientX - this.panning.x;
      this.panY = this.panning.py + ev.clientY - this.panning.y;
      this.onChange();
    };
    const up = () => {
      this.panning = null;
      this.el.classList.remove('panning');
      this.el.removeEventListener('pointermove', move);
      this.el.removeEventListener('pointerup', up);
      this.el.removeEventListener('pointercancel', up);
    };
    this.el.addEventListener('pointermove', move);
    this.el.addEventListener('pointerup', up);
    this.el.addEventListener('pointercancel', up);
    return true;
  }

  get zoomLabel(): string {
    return `${Math.round(this.zoom * 100)}%`;
  }

  /** Sizes the display canvas backing store (capped) to the on-screen rect and returns its 2D context. */
  prepareDisplay(maxSide = 1600): { ctx: CanvasRenderingContext2D; w: number; h: number } | null {
    const r = this.rect();
    const k = Math.min(1, maxSide / Math.max(r.w * this.dpr, r.h * this.dpr));
    const w = Math.max(2, Math.round(r.w * this.dpr * k));
    const hh = Math.max(2, Math.round(r.h * this.dpr * k));
    if (this.display.width !== w || this.display.height !== hh) {
      this.display.width = w;
      this.display.height = hh;
    }
    const ctx = this.display.getContext('2d');
    return ctx ? { ctx, w, h: hh } : null;
  }
}
