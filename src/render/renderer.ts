// WebGL2 renderer: one ContentSource per content layer, rendered offscreen at the
// size the layer occupies, then composited with a true perspective homography,
// shape / crop / feather / blend / masks, brightness and the test pattern.

import { RenderTarget } from '../gl/renderTarget';
import { drawQuad } from '../gl/quad';
import { invert, isConvex, squareToQuad, toGl } from '../geometry/homography';
import type { BlendMode, ContentLayer, MaskLayer, Project, Quad, Scene } from '../state/types';
import type { AssetResolver, ClockSnapshot, CreateRenderer, LayerClock, LayerStatus, Renderer, RenderStats } from './api';
import { MaskCache } from './masks';
import { createPrograms, type Programs } from './programs';
import { bucket, parseColor, type ContentSource, type FrameInfo, type SizeRequest, type SourceOutput } from './source';
import { createSource } from './sources';
import { TestPattern } from './testPattern';

const MAX_LAYER_MASKS = 4;
const MIN_SIZE = 16;
const MAX_SIZE = 4096;

interface Geom {
  key: Float64Array;
  ok: boolean;
  sized: boolean;
  /** Corners scaled to the render size. */
  q: Quad;
  inv: Float32Array<ArrayBuffer>;
  clip: Float32Array;
  quadW: number;
  quadH: number;
  radius: number;
  feather: number;
  req: SizeRequest;
}

interface LayerRt {
  layer: ContentLayer;
  source: ContentSource;
  geom: Geom;
  out: SourceOutput | null;
  outTick: number;
  /** Set by the output pass: this layer has a texture to composite this frame. */
  drawn: boolean;
  /** Last-resort error from a render that threw; cleared on the next project change. */
  error: LayerStatus | null;
  /** Key of the inputs of the canonical generated-content size (geom.req). */
  reqKey: Float64Array;
  /** Last clock snapshot handed to the source. */
  clockRef: ClockRef | null;
}

interface ClockRef {
  clock: LayerClock;
  at: number;
}

function newGeom(): Geom {
  return {
    key: new Float64Array(15).fill(NaN),
    ok: false,
    sized: false,
    q: [[0, 0], [0, 0], [0, 0], [0, 0]],
    inv: new Float32Array(9),
    clip: new Float32Array(8),
    quadW: 1,
    quadH: 1,
    radius: 0,
    feather: 0,
    req: { w: MIN_SIZE, h: MIN_SIZE, bw: 32, bh: 32, refW: 1, refH: 1 },
  };
}

const clampSize = (n: number) => Math.min(MAX_SIZE, Math.max(MIN_SIZE, Math.round(n)));
const dist = (a: readonly number[], b: readonly number[]) => Math.hypot(a[0] - b[0], a[1] - b[1]);

function sameStatus(a: LayerStatus, b: LayerStatus): boolean {
  if (a.state !== b.state) return false;
  return a.state !== 'error' || a.message === (b as { message: string }).message;
}

class WebGlRenderer implements Renderer {
  readonly stats: RenderStats = { fps: 0, layerMs: {} };
  onStatusChange: ((layerId: string, status: LayerStatus) => void) | null = null;

  private readonly gl: WebGL2RenderingContext;
  private readonly programs: Programs;
  private readonly masks: MaskCache;
  private readonly testPattern: TestPattern;
  private readonly comp: RenderTarget;
  private readonly emptyVao: WebGLVertexArrayObject;
  private readonly dummyTex: WebGLTexture;

  private project: Project | null = null;
  private scene: Scene | null = null;
  private readonly layers = new Map<string, LayerRt>();
  private readonly statuses = new Map<string, LayerStatus>();
  private targeted = new Map<string, MaskLayer[]>();

  private tickCount = 1;
  private lastNow: number | null = null;
  private readonly frame: FrameInfo & { date: [number, number, number, number] } = {
    dt: 0,
    absMs: 0,
    globalSpeed: 1,
    globalPaused: false,
    date: [0, 0, 0, 0],
  };
  private region = { w: 0, h: 0 };
  private readonly clockRefs = new Map<string, ClockRef>();
  /** Machine-wide time (ms) of the last tick; the moment the clocks returned by getClocks() describe. */
  private tickAbs = 0;
  private lost = false;
  private readonly scratchKey = new Float64Array(15);
  private readonly maskTex: (WebGLTexture | null)[] = [null, null, null, null];
  private readonly maskOpacity = new Float32Array(4);

  private readonly onLost = (e: Event) => {
    e.preventDefault();
    this.lost = true;
    const status: LayerStatus = { state: 'error', message: 'WebGL context lost. Reload the page to recover.' };
    console.warn('[render] WebGL context lost');
    for (const id of this.layers.keys()) this.setStatus(id, status);
  };
  private readonly onRestored = () => {
    // Shared helpers (gl/quad, isf/compile) cache GL objects per context, so a restored
    // context can't be reused safely; the owner has to recreate the canvas and renderer.
    console.warn('[render] WebGL context restored, but this renderer must be recreated');
  };

  constructor(
    readonly canvas: HTMLCanvasElement,
    private readonly assets: AssetResolver,
  ) {
    const gl = canvas.getContext('webgl2', {
      alpha: false,
      antialias: false,
      premultipliedAlpha: false,
      preserveDrawingBuffer: false,
      powerPreference: 'high-performance',
    });
    if (!gl) throw new Error('WebGL 2 is not available in this browser.');
    this.gl = gl;
    canvas.addEventListener('webglcontextlost', this.onLost);
    canvas.addEventListener('webglcontextrestored', this.onRestored);
    gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, true);
    gl.pixelStorei(gl.UNPACK_PREMULTIPLY_ALPHA_WEBGL, true);
    gl.disable(gl.DEPTH_TEST);
    gl.disable(gl.CULL_FACE);
    this.programs = createPrograms(gl);
    this.masks = new MaskCache(gl);
    this.testPattern = new TestPattern(gl);
    this.comp = new RenderTarget(gl);
    this.emptyVao = gl.createVertexArray()!;
    this.dummyTex = gl.createTexture()!;
    gl.bindTexture(gl.TEXTURE_2D, this.dummyTex);
    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA8, 1, 1, 0, gl.RGBA, gl.UNSIGNED_BYTE, new Uint8Array([255, 255, 255, 255]));
    for (let i = 1; i <= MAX_LAYER_MASKS; i++) {
      gl.activeTexture(gl.TEXTURE0 + i);
      gl.bindTexture(gl.TEXTURE_2D, this.dummyTex);
    }
    gl.activeTexture(gl.TEXTURE0);
  }

  // Project reconciliation

  setProject(project: Project): void {
    if (this.lost) return;
    try {
      this.project = project;
      const scene = project.scenes.find((s) => s.id === project.activeSceneId) ?? project.scenes[0] ?? null;
      this.scene = scene;
      const contentLayers = new Map<string, ContentLayer>();
      const masks: MaskLayer[] = [];
      for (const l of scene?.layers ?? []) {
        if (l.kind === 'content') contentLayers.set(l.id, l);
        else masks.push(l);
      }
      for (const [id, rt] of this.layers) {
        const next = contentLayers.get(id);
        if (next && next.content.type === rt.layer.content.type) continue;
        rt.source.dispose();
        this.layers.delete(id);
        this.statuses.delete(id);
      }
      for (const [id, layer] of contentLayers) {
        let rt = this.layers.get(id);
        if (rt) {
          rt.layer = layer;
          rt.error = null;
          if (!rt.source.update(layer.content)) {
            rt.source.dispose();
            rt.source = createSource({ gl: this.gl, assets: this.assets }, layer.content);
          }
        } else {
          rt = { layer, source: createSource({ gl: this.gl, assets: this.assets }, layer.content), geom: newGeom(), out: null, outTick: 0, drawn: false, error: null, reqKey: new Float64Array(9).fill(NaN), clockRef: null };
          this.layers.set(id, rt);
        }
      }
      this.masks.sync(masks);
      this.targeted = new Map();
      for (const m of masks) {
        if (m.target === 'below' || !contentLayers.has(m.target)) continue;
        const list = this.targeted.get(m.target);
        if (list) list.push(m);
        else this.targeted.set(m.target, [m]);
      }
      this.pollStatuses();
    } catch (e) {
      console.error('[render] setProject failed', e);
    }
  }

  tick(nowMs: number): void {
    if (this.lost) return;
    try {
      const real = this.lastNow === null ? 0 : (nowMs - this.lastNow) / 1000;
      this.lastNow = nowMs;
      if (real > 0) {
        const inst = Math.min(1000, 1 / real);
        this.stats.fps = this.stats.fps === 0 ? inst : this.stats.fps * 0.9 + inst * 0.1;
      }
      const f = this.frame;
      f.dt = Math.min(Math.max(real, 0), 0.1);
      f.globalSpeed = this.project?.playback.speed ?? 1;
      f.globalPaused = this.project?.playback.paused ?? false;
      const d = new Date();
      f.date[0] = d.getFullYear();
      f.date[1] = d.getMonth() + 1;
      f.date[2] = d.getDate();
      f.date[3] = d.getHours() * 3600 + d.getMinutes() * 60 + d.getSeconds() + d.getMilliseconds() / 1000;
      this.tickCount++;
      this.tickAbs = f.absMs = performance.timeOrigin + nowMs;
      for (const [id, rt] of this.layers) {
        try {
          const ref = this.clockRefs.get(id);
          if (ref && ref !== rt.clockRef) {
            rt.clockRef = ref;
            rt.source.syncClock?.(ref.clock, ref.at);
          }
          rt.source.tick(f);
        } catch (e) {
          rt.error = { state: 'error', message: e instanceof Error ? e.message : String(e) };
        }
      }
      this.pollStatuses();
    } catch (e) {
      console.error('[render] tick failed', e);
    }
  }

  // Clocks

  getClocks(): ClockSnapshot {
    const layers: Record<string, LayerClock> = {};
    for (const [id, rt] of this.layers) {
      const c = rt.source.getClock?.();
      if (c) layers[id] = c;
    }
    return { at: this.tickAbs, layers };
  }

  syncClocks(layers: Record<string, LayerClock>, sentAtAbsMs: number): void {
    // Kept by id (not by source) so a layer that is created or rebuilt later still picks it up.
    this.clockRefs.clear();
    for (const id in layers) this.clockRefs.set(id, { clock: layers[id], at: sentAtAbsMs });
  }

  // Status

  status(layerId: string): LayerStatus {
    const rt = this.layers.get(layerId);
    return rt ? (rt.error ?? rt.source.status()) : { state: 'ok' };
  }

  private pollStatuses(): void {
    for (const [id, rt] of this.layers) this.setStatus(id, rt.error ?? rt.source.status());
  }

  private setStatus(id: string, s: LayerStatus): void {
    const prev = this.statuses.get(id);
    if (prev && sameStatus(prev, s)) return;
    this.statuses.set(id, s);
    try {
      this.onStatusChange?.(id, s);
    } catch (e) {
      console.error('[render] onStatusChange threw', e);
    }
  }

  contentSize(layerId: string): { width: number; height: number } | null {
    return this.layers.get(layerId)?.source.nativeSize() ?? null;
  }

  // Geometry

  /** Recomputes homography, clip corners and the generated-content size when anything they depend on changed. */
  private geometry(rt: LayerRt, rw: number, rh: number): Geom {
    const g = rt.geom;
    const l = rt.layer;
    const p = this.project!;
    const cw = p.canvas.width;
    const ch = p.canvas.height;
    const k = this.scratchKey;
    k[0] = rw;
    k[1] = rh;
    k[2] = cw;
    k[3] = ch;
    for (let i = 0; i < 4; i++) {
      k[4 + i * 2] = l.corners[i][0];
      k[5 + i * 2] = l.corners[i][1];
    }
    k[12] = l.quality;
    k[13] = l.cornerRadius;
    k[14] = l.edgeFeather;
    let same = true;
    for (let i = 0; i < 15; i++) {
      if (k[i] !== g.key[i]) {
        same = false;
        break;
      }
    }
    if (same) return g;
    g.key.set(k);

    const c = l.corners;
    const sx = rw / cw;
    const sy = rh / ch;
    for (let i = 0; i < 4; i++) {
      g.q[i][0] = c[i][0] * sx;
      g.q[i][1] = c[i][1] * sy;
    }
    let finite = true;
    for (let i = 4; i < 15; i++) if (!Number.isFinite(k[i])) finite = false;
    const inv = finite && isConvex(g.q) ? invert(squareToQuad(g.q)) : null;
    g.ok = inv !== null && cw > 0 && ch > 0;
    if (!inv) return g;
    toGl(inv, g.inv);
    const q = g.q;
    const order = [0, 1, 3, 2];
    for (let i = 0; i < 4; i++) {
      g.clip[i * 2] = (q[order[i]][0] / rw) * 2 - 1;
      g.clip[i * 2 + 1] = 1 - (q[order[i]][1] / rh) * 2;
    }
    g.quadW = (dist(q[0], q[1]) + dist(q[3], q[2])) / 2;
    g.quadH = (dist(q[0], q[3]) + dist(q[1], q[2])) / 2;
    const s = (sx + sy) / 2;
    g.radius = l.cornerRadius * s;
    g.feather = l.edgeFeather * s;

    return g;
  }

  /**
   * Canonical size of a generated source: the layer's longest opposite sides in PROJECT
   * pixels times its quality. Deliberately independent of the window / panel being drawn,
   * so the editor preview and the projector run the shader at exactly the same RENDERSIZE.
   */
  private sizeRequest(rt: LayerRt): SizeRequest {
    const l = rt.layer;
    const c = l.corners;
    const k = rt.reqKey;
    let same = true;
    const next = [c[0][0], c[0][1], c[1][0], c[1][1], c[2][0], c[2][1], c[3][0], c[3][1], l.quality];
    for (let i = 0; i < 9; i++) {
      if (next[i] !== k[i]) same = false;
      k[i] = next[i];
    }
    const r = rt.geom.req;
    if (same && rt.geom.sized) return r;
    const fin = (n: number, d: number) => (Number.isFinite(n) ? n : d);
    const wp = fin(Math.max(dist(c[0], c[1]), dist(c[3], c[2])), MIN_SIZE);
    const hp = fin(Math.max(dist(c[0], c[3]), dist(c[1], c[2])), MIN_SIZE);
    const scale = Math.max(0.05, fin(l.quality, 1));
    r.w = clampSize(wp * scale);
    r.h = clampSize(hp * scale);
    r.bw = bucket(r.w);
    r.bh = bucket(r.h);
    r.refW = Math.max(1, wp);
    r.refH = Math.max(1, hp);
    rt.geom.sized = true;
    return r;
  }

  /** Renders a layer's source at most once per tick. */
  private output(rt: LayerRt): SourceOutput | null {
    if (rt.outTick === this.tickCount) return rt.out;
    rt.outTick = this.tickCount;
    try {
      rt.out = rt.source.render(this.sizeRequest(rt));
    } catch (e) {
      rt.out = null;
      rt.error = { state: 'error', message: e instanceof Error ? e.message : String(e) };
    }
    return rt.out;
  }

  // Drawing

  private growCanvas(w: number, h: number): void {
    const c = this.canvas;
    if (c.width < w) c.width = w;
    if (c.height < h) c.height = h;
  }

  renderOutput(width: number, height: number): void {
    if (this.lost) return;
    try {
      this.drawOutput(Math.max(1, Math.round(width)), Math.max(1, Math.round(height)));
    } catch (e) {
      console.error('[render] renderOutput failed', e);
    }
  }

  private drawOutput(w: number, h: number): void {
    const gl = this.gl;
    this.growCanvas(w, h);
    this.region.w = w;
    this.region.h = h;
    const p = this.project;
    if (!p || p.output.blackout) {
      gl.bindFramebuffer(gl.FRAMEBUFFER, null);
      gl.viewport(0, 0, w, h);
      gl.clearColor(0, 0, 0, 1);
      gl.clear(gl.COLOR_BUFFER_BIT);
      return;
    }
    const layers = this.scene?.layers ?? [];

    // Pass 1: render every visible layer's content (offscreen, no blending).
    gl.disable(gl.BLEND);
    for (const l of layers) {
      if (l.kind !== 'content') continue;
      const rt = this.layers.get(l.id);
      if (!rt) continue;
      rt.drawn = false;
      if (!l.visible || l.opacity <= 0) continue;
      const g = this.geometry(rt, w, h);
      if (!g.ok) continue;
      rt.drawn = this.output(rt) !== null;
    }

    // Pass 2: composite into the final target.
    this.comp.ensureSize(w, h);
    this.comp.bind();
    const bg = parseColor(p.output.background);
    gl.clearColor(bg[0], bg[1], bg[2], 1);
    gl.clear(gl.COLOR_BUFFER_BIT);
    gl.enable(gl.BLEND);
    for (const l of layers) {
      if (!l.visible || l.opacity <= 0) continue;
      if (l.kind === 'content') {
        const rt = this.layers.get(l.id);
        if (rt && rt.drawn && rt.out) this.drawLayer(rt, w, h);
      } else if (l.target === 'below') {
        this.drawBelowMask(l, bg, w, h);
      }
    }

    // Final pass: brightness (once), then the test pattern, onto the canvas.
    gl.bindFramebuffer(gl.FRAMEBUFFER, null);
    gl.viewport(0, 0, w, h);
    gl.disable(gl.BLEND);
    const B = this.programs.blit;
    gl.useProgram(B.program);
    gl.activeTexture(gl.TEXTURE0);
    gl.bindTexture(gl.TEXTURE_2D, this.comp.texture);
    this.setBlit(1, 1, 0, 0, 1, 1, true, false, p.output.brightness);
    drawQuad(gl);
    if (p.output.testPattern) {
      this.testPattern.ensure(w, h, p.canvas.width, p.canvas.height);
      gl.enable(gl.BLEND);
      gl.blendFuncSeparate(gl.ONE, gl.ONE_MINUS_SRC_ALPHA, gl.ZERO, gl.ONE);
      gl.bindTexture(gl.TEXTURE_2D, this.testPattern.texture);
      this.setBlit(1, 1, 0, 0, 1, 1, true, false, 1);
      drawQuad(gl);
      gl.disable(gl.BLEND);
    }
  }

  private setBlit(sx: number, sy: number, minX: number, minY: number, maxX: number, maxY: number, premul: boolean, checker: boolean, brightness: number, useTex = true): void {
    const gl = this.gl;
    const u = this.programs.blit.u;
    gl.uniform2f(u.u_texScale, sx, sy);
    gl.uniform4f(u.u_texRect, minX, minY, maxX, maxY);
    gl.uniform1f(u.u_premul, premul ? 1 : 0);
    gl.uniform1f(u.u_checker, checker ? 1 : 0);
    gl.uniform1f(u.u_useTex, useTex ? 1 : 0);
    gl.uniform1f(u.u_brightness, Math.min(1, Math.max(0, brightness)));
  }

  private setBlend(mode: BlendMode): void {
    const gl = this.gl;
    switch (mode) {
      case 'add':
        gl.blendFuncSeparate(gl.ONE, gl.ONE, gl.ZERO, gl.ONE);
        break;
      case 'screen':
        gl.blendFuncSeparate(gl.ONE_MINUS_DST_COLOR, gl.ONE, gl.ZERO, gl.ONE);
        break;
      case 'multiply':
        gl.blendFuncSeparate(gl.DST_COLOR, gl.ONE_MINUS_SRC_ALPHA, gl.ZERO, gl.ONE);
        break;
      default:
        gl.blendFuncSeparate(gl.ONE, gl.ONE_MINUS_SRC_ALPHA, gl.ZERO, gl.ONE);
    }
  }

  private drawLayer(rt: LayerRt, w: number, h: number): void {
    const gl = this.gl;
    const l = rt.layer;
    const g = rt.geom;
    const out = rt.out!;
    const L = this.programs.layer;
    const u = L.u;
    const p = this.project!;
    gl.useProgram(L.program);
    gl.bindVertexArray(this.emptyVao);
    gl.activeTexture(gl.TEXTURE0);
    gl.bindTexture(gl.TEXTURE_2D, out.texture);
    this.setBlend(l.blend);
    gl.uniform2fv(u.u_c, g.clip);
    gl.uniformMatrix3fv(u.u_inv, false, g.inv);
    gl.uniform2f(u.u_size, w, h);
    gl.uniform4f(u.u_crop, l.crop.x, l.crop.y, l.crop.w, l.crop.h);
    gl.uniform2f(u.u_texScale, out.scaleX, out.scaleY);
    gl.uniform4f(u.u_texRect, out.minX, out.minY, out.maxX, out.maxY);
    gl.uniform1f(u.u_premul, out.premultiplied ? 1 : 0);
    gl.uniform1f(u.u_flipX, rt.source.mirrorX ? 1 : 0);
    gl.uniform1f(u.u_ellipse, l.shape === 'ellipse' ? 1 : 0);
    gl.uniform2f(u.u_quadSize, g.quadW, g.quadH);
    gl.uniform1f(u.u_radius, l.shape === 'rect' ? g.radius : 0);
    gl.uniform1f(u.u_feather, g.feather);
    gl.uniform1f(u.u_opacity, Math.min(1, l.opacity));

    let n = 0;
    const list = this.targeted.get(l.id);
    if (list) {
      for (const m of list) {
        if (!m.visible || m.opacity <= 0 || n >= MAX_LAYER_MASKS) continue;
        const tex = this.masks.texture(m.id, w, h, p.canvas.width, p.canvas.height);
        if (!tex) continue;
        this.maskTex[n] = tex;
        this.maskOpacity[n] = Math.min(1, m.opacity);
        n++;
      }
    }
    for (let i = 0; i < n; i++) {
      gl.activeTexture(gl.TEXTURE1 + i);
      gl.bindTexture(gl.TEXTURE_2D, this.maskTex[i]);
    }
    gl.activeTexture(gl.TEXTURE0);
    gl.uniform1i(u.u_maskCount, n);
    gl.uniform4fv(u.u_maskOpacity, this.maskOpacity);
    gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
  }

  private drawBelowMask(m: MaskLayer, bg: [number, number, number], w: number, h: number): void {
    const gl = this.gl;
    const p = this.project!;
    const tex = this.masks.texture(m.id, w, h, p.canvas.width, p.canvas.height);
    if (!tex) return;
    const M = this.programs.mask;
    gl.useProgram(M.program);
    gl.activeTexture(gl.TEXTURE0);
    gl.bindTexture(gl.TEXTURE_2D, tex);
    gl.blendFuncSeparate(gl.ONE, gl.ONE_MINUS_SRC_ALPHA, gl.ZERO, gl.ONE);
    gl.uniform3f(M.u.u_bg, bg[0], bg[1], bg[2]);
    gl.uniform1f(M.u.u_opacity, Math.min(1, m.opacity));
    drawQuad(gl);
  }

  renderContent(layerId: string, width: number, height: number): void {
    if (this.lost) return;
    try {
      const w = Math.max(1, Math.round(width));
      const h = Math.max(1, Math.round(height));
      const gl = this.gl;
      this.growCanvas(w, h);
      this.region.w = w;
      this.region.h = h;
      gl.bindFramebuffer(gl.FRAMEBUFFER, null);
      gl.viewport(0, 0, w, h);
      gl.disable(gl.BLEND);
      const rt = this.layers.get(layerId);
      let out: SourceOutput | null = null;
      if (rt) {
        out = this.output(rt);
        // Drawing back to the default framebuffer: restore it after any offscreen render.
        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        gl.viewport(0, 0, w, h);
      }
      const B = this.programs.blit;
      gl.useProgram(B.program);
      gl.activeTexture(gl.TEXTURE0);
      if (out) {
        gl.bindTexture(gl.TEXTURE_2D, out.texture);
        this.setBlit(out.scaleX, out.scaleY, out.minX, out.minY, out.maxX, out.maxY, out.premultiplied, true, 1);
      } else {
        this.setBlit(1, 1, 0, 0, 1, 1, true, true, 1, false);
      }
      drawQuad(gl);
    } catch (e) {
      console.error('[render] renderContent failed', e);
    }
  }

  copyTo(ctx: CanvasRenderingContext2D): void {
    const { w, h } = this.region;
    if (this.lost || w === 0 || h === 0) return;
    const c = this.canvas;
    ctx.drawImage(c, 0, c.height - h, w, h, 0, 0, ctx.canvas.width, ctx.canvas.height);
  }

  dispose(): void {
    this.canvas.removeEventListener('webglcontextlost', this.onLost);
    this.canvas.removeEventListener('webglcontextrestored', this.onRestored);
    for (const rt of this.layers.values()) rt.source.dispose();
    this.layers.clear();
    this.statuses.clear();
    this.masks.dispose();
    this.testPattern.dispose();
    this.comp.dispose();
  }
}

export const createRenderer: CreateRenderer = (canvas, assets) => new WebGlRenderer(canvas, assets);
