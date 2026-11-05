// Mask polygons rasterised to single-channel textures at render resolution.
// Texture value 1 = keep, 0 = cut. Rebuilt only when points / feather / invert / size change.

import type { MaskLayer } from '../state/types';
import { makeTexture } from './source';

interface Entry {
  mask: MaskLayer;
  /** Signature of the mask's geometry props, refreshed on every project change. */
  sig: string;
  builtSig: string;
  builtW: number;
  builtH: number;
  projW: number;
  projH: number;
  texture: WebGLTexture;
}

export class MaskCache {
  private readonly entries = new Map<string, Entry>();
  private readonly canvas = document.createElement('canvas');
  private readonly ctx = this.canvas.getContext('2d', { willReadFrequently: false })!;

  constructor(private readonly gl: WebGL2RenderingContext) {}

  /** Reconciles with the masks of the active scene. */
  sync(masks: MaskLayer[]): void {
    const live = new Set<string>();
    for (const mask of masks) {
      live.add(mask.id);
      const sig = `${mask.invert ? 1 : 0}|${mask.feather}|${mask.points.map((p) => `${p[0]},${p[1]}`).join(';')}`;
      let e = this.entries.get(mask.id);
      if (!e) {
        e = { mask, sig, builtSig: '', builtW: 0, builtH: 0, projW: 0, projH: 0, texture: makeTexture(this.gl) };
        this.entries.set(mask.id, e);
      }
      e.mask = mask;
      e.sig = sig;
    }
    for (const [id, e] of this.entries) {
      if (live.has(id)) continue;
      this.gl.deleteTexture(e.texture);
      this.entries.delete(id);
    }
  }

  /** The keep-texture for a mask at the given render size and project canvas size. */
  texture(id: string, w: number, h: number, projW: number, projH: number): WebGLTexture | null {
    const e = this.entries.get(id);
    if (!e) return null;
    if (e.builtSig !== e.sig || e.builtW !== w || e.builtH !== h || e.projW !== projW || e.projH !== projH) this.build(e, w, h, projW, projH);
    return e.texture;
  }

  private build(e: Entry, w: number, h: number, projW: number, projH: number): void {
    const { canvas, ctx, gl } = this;
    const m = e.mask;
    if (canvas.width !== w) canvas.width = w;
    if (canvas.height !== h) canvas.height = h;
    const sx = w / projW;
    const sy = h / projH;
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.filter = 'none';
    ctx.fillStyle = m.invert ? '#000' : '#fff';
    ctx.fillRect(0, 0, w, h);
    if (m.points.length >= 3) {
      const blur = (m.feather * (sx + sy)) / 4;
      ctx.filter = blur > 0.01 ? `blur(${blur}px)` : 'none';
      ctx.fillStyle = m.invert ? '#fff' : '#000';
      ctx.beginPath();
      m.points.forEach(([x, y], i) => (i === 0 ? ctx.moveTo(x * sx, y * sy) : ctx.lineTo(x * sx, y * sy)));
      ctx.closePath();
      ctx.fill();
      ctx.filter = 'none';
    }
    gl.bindTexture(gl.TEXTURE_2D, e.texture);
    if (e.builtW === w && e.builtH === h) {
      gl.texSubImage2D(gl.TEXTURE_2D, 0, 0, 0, gl.RED, gl.UNSIGNED_BYTE, canvas);
    } else {
      gl.texImage2D(gl.TEXTURE_2D, 0, gl.R8, gl.RED, gl.UNSIGNED_BYTE, canvas);
    }
    e.builtSig = e.sig;
    e.builtW = w;
    e.builtH = h;
    e.projW = projW; e.projH = projH;
  }

  dispose(): void {
    for (const e of this.entries.values()) this.gl.deleteTexture(e.texture);
    this.entries.clear();
  }
}
