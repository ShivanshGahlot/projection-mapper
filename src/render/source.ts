// The contract between the compositor and one layer's content. The compositor
// only ever sees ContentSource; new content types plug in via sources/index.ts.
//
// Texture convention for every source: texel row 0 is the BOTTOM of the content
// (GL convention). Uploads from DOM elements use UNPACK_FLIP_Y_WEBGL (set once
// globally by the renderer); render targets are y-up by nature.

import type { Content } from '../state/types';
import type { AssetResolver, LayerStatus } from './api';

export const STATUS_OK: LayerStatus = { state: 'ok' };
export const STATUS_LOADING: LayerStatus = { state: 'loading' };

export interface SourceEnv {
  gl: WebGL2RenderingContext;
  assets: AssetResolver;
}

export interface FrameInfo {
  /** Real seconds since the previous tick, clamped. */
  dt: number;
  globalSpeed: number;
  globalPaused: boolean;
  /** year, month (1-12), day, seconds since midnight */
  date: readonly [number, number, number, number];
}

/** Size a generated source should render at. All values in pixels. */
export interface SizeRequest {
  /** Exact target size. */
  w: number;
  h: number;
  /** Bucketed (multiple of 32) size; use for allocations and cheap-to-change content. */
  bw: number;
  bh: number;
  /** Size of the layer in project pixels, the reference frame for text layout. */
  refW: number;
  refH: number;
}

/** What a source hands the compositor. Mutated in place; never allocated per frame. */
export interface SourceOutput {
  texture: WebGLTexture;
  /** false: rgb is straight alpha, the compositor premultiplies. */
  premultiplied: boolean;
  /** Content occupies [0, scale] of the texture (sub-viewport rendering). */
  scaleX: number;
  scaleY: number;
  /** Sampling clamp rectangle in texture coordinates (min x/y, max x/y). */
  minX: number;
  minY: number;
  maxX: number;
  maxY: number;
}

export interface ContentSource {
  /** True when the content is rendered at the size the layer occupies. */
  readonly generated: boolean;
  /** Flip the content horizontally (webcam mirror). */
  readonly mirrorX: boolean;
  /** Apply new content props. Returns false when this source cannot represent them (recreate it). */
  update(content: Content): boolean;
  tick(frame: FrameInfo): void;
  /** Texture for this frame, or null when nothing can be drawn (transparent / not ready / error). */
  render(size: SizeRequest): SourceOutput | null;
  /** Native pixel size for media, null for generated content. */
  nativeSize(): { width: number; height: number } | null;
  status(): LayerStatus;
  dispose(): void;
}

export function newOutput(): SourceOutput {
  return {
    texture: null as unknown as WebGLTexture,
    premultiplied: true,
    scaleX: 1,
    scaleY: 1,
    minX: 0,
    minY: 0,
    maxX: 1,
    maxY: 1,
  };
}

export function setFullOutput(out: SourceOutput, texture: WebGLTexture, premultiplied: boolean): SourceOutput {
  out.texture = texture;
  out.premultiplied = premultiplied;
  out.scaleX = out.scaleY = 1;
  out.minX = out.minY = 0;
  out.maxX = out.maxY = 1;
  return out;
}

export function makeTexture(gl: WebGL2RenderingContext, mipmaps = false): WebGLTexture {
  const t = gl.createTexture()!;
  gl.bindTexture(gl.TEXTURE_2D, t);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, mipmaps ? gl.LINEAR_MIPMAP_LINEAR : gl.LINEAR);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
  return t;
}

/** #rgb / #rrggbb to 0-1 components; falls back to black. */
export function parseColor(c: string): [number, number, number] {
  let h = c.trim().replace('#', '');
  if (h.length === 3) h = h[0] + h[0] + h[1] + h[1] + h[2] + h[2];
  const n = parseInt(h.slice(0, 6), 16);
  if (!Number.isFinite(n) || h.length < 6) return [0, 0, 0];
  return [((n >> 16) & 255) / 255, ((n >> 8) & 255) / 255, (n & 255) / 255];
}

export function errorStatus(e: unknown): LayerStatus {
  return { state: 'error', message: e instanceof Error ? e.message : String(e) };
}

/** Rounds up to a multiple of 32, never above 4096. */
export function bucket(n: number): number {
  return Math.min(4096, Math.ceil(n / 32) * 32);
}
