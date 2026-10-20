// Owns the WebGL context and the frame loop: ticks every layer, renders the
// visible ones into their own textures and composites them onto the canvas.

import { createProgram } from '../gl/program';
import { drawQuad } from '../gl/quad';
import { ShaderLayer } from '../layers/shaderLayer';
import type { AudioValues, FrameContext, LayerRuntime, LayerStatus } from '../layers/layer';
import type { ProjectState } from '../state/types';

const BLIT_VS = `#version 300 es
in vec2 a_pos;
out vec2 v_uv;
void main() {
  v_uv = a_pos * 0.5 + 0.5;
  gl_Position = vec4(a_pos, 0.0, 1.0);
}`;

// Output is premultiplied by opacity; blending is ONE, ONE_MINUS_SRC_ALPHA.
const BLIT_FS = `#version 300 es
precision highp float;
in vec2 v_uv;
uniform sampler2D u_tex;
uniform float u_opacity;
out vec4 outColor;
void main() {
  outColor = vec4(texture(u_tex, v_uv).rgb, 1.0) * u_opacity;
}`;

function sameStatus(a: LayerStatus | undefined, b: LayerStatus): boolean {
  if (!a || a.state !== b.state) return false;
  return a.state !== 'error' || a.message === (b as { message: string }).message;
}

export class Engine {
  readonly gl: WebGL2RenderingContext;
  /** Smoothed frames per second. */
  fps = 0;
  onStatusChange: ((layerId: string, status: LayerStatus) => void) | null = null;

  private project: ProjectState | null = null;
  private readonly runtimes = new Map<string, LayerRuntime>();
  private readonly statuses = new Map<string, LayerStatus>();
  private readonly blit: WebGLProgram;
  private readonly blitOpacity: WebGLUniformLocation;
  private frameIndex = 0;
  private lastTime = 0;
  private raf = 0;
  private readonly audio: AudioValues = { bass: 0, mid: 0, high: 0, beat: 0, level: 0 };
  private readonly date: [number, number, number, number] = [0, 0, 0, 0];
  private readonly ctx: FrameContext = { dt: 0, frameIndex: 0, date: this.date, audio: this.audio };

  constructor(private readonly canvas: HTMLCanvasElement) {
    const gl = canvas.getContext('webgl2', {
      alpha: false,
      antialias: false,
      premultipliedAlpha: false,
      powerPreference: 'high-performance',
    });
    if (!gl) throw new Error('WebGL 2 is not available in this browser.');
    this.gl = gl;
    this.blit = createProgram(gl, BLIT_VS, BLIT_FS);
    this.blitOpacity = gl.getUniformLocation(this.blit, 'u_opacity')!;
    gl.useProgram(this.blit);
    gl.uniform1i(gl.getUniformLocation(this.blit, 'u_tex'), 0);
    gl.enable(gl.BLEND);
    gl.blendFunc(gl.ONE, gl.ONE_MINUS_SRC_ALPHA);
  }

  /** Reconciles the GPU layers with the project state (create, update, dispose by id). */
  sync(project: ProjectState): void {
    this.project = project;
    const { outputWidth, outputHeight } = project.settings;
    if (this.canvas.width !== outputWidth) this.canvas.width = outputWidth;
    if (this.canvas.height !== outputHeight) this.canvas.height = outputHeight;

    const ids = new Set(project.layers.map((l) => l.id));
    for (const [id, rt] of this.runtimes) {
      if (ids.has(id)) continue;
      rt.dispose();
      this.runtimes.delete(id);
      this.statuses.delete(id);
    }
    for (const layer of project.layers) {
      const rt = this.runtimes.get(layer.id);
      if (rt) rt.update(layer);
      else this.runtimes.set(layer.id, new ShaderLayer(this.gl, layer));
    }
  }

  start(): void {
    if (this.raf) return;
    this.lastTime = performance.now();
    this.raf = requestAnimationFrame(this.frame);
  }

  stop(): void {
    cancelAnimationFrame(this.raf);
    this.raf = 0;
  }

  private frame = (now: number): void => {
    this.raf = requestAnimationFrame(this.frame);
    const real = (now - this.lastTime) / 1000;
    this.lastTime = now;
    if (real > 0) this.fps = this.fps === 0 ? 1 / real : this.fps * 0.95 + (1 / real) * 0.05;
    const project = this.project;
    if (!project) return;

    const { globalSpeed, globalPaused, outputWidth, outputHeight } = project.settings;
    const dt = Math.min(Math.max(real, 0), 0.1);
    const scaled = globalPaused ? 0 : dt * globalSpeed;
    const d = new Date();
    this.date[0] = d.getFullYear();
    this.date[1] = d.getMonth() + 1;
    this.date[2] = d.getDate();
    this.date[3] = d.getHours() * 3600 + d.getMinutes() * 60 + d.getSeconds() + d.getMilliseconds() / 1000;
    this.ctx.dt = dt;
    this.ctx.frameIndex = this.frameIndex++;

    const gl = this.gl;
    for (const rt of this.runtimes.values()) rt.tick(scaled);

    // Render every visible layer first, then composite, so the render targets
    // never interleave with drawing to the canvas.
    gl.disable(gl.BLEND);
    const rendered: { tex: WebGLTexture; opacity: number }[] = [];
    for (const layer of project.layers) {
      const rt = this.runtimes.get(layer.id);
      if (!rt) continue;
      const tex = layer.visible
        ? rt.render(this.ctx, outputWidth * layer.quality, outputHeight * layer.quality)
        : null;
      this.reportStatus(layer.id, rt.status);
      if (tex) rendered.push({ tex, opacity: layer.opacity });
    }

    gl.bindFramebuffer(gl.FRAMEBUFFER, null);
    gl.viewport(0, 0, this.canvas.width, this.canvas.height);
    gl.clearColor(0, 0, 0, 1);
    gl.clear(gl.COLOR_BUFFER_BIT);
    if (rendered.length === 0) return;
    gl.useProgram(this.blit);
    gl.enable(gl.BLEND);
    gl.activeTexture(gl.TEXTURE0);
    for (const r of rendered) {
      gl.bindTexture(gl.TEXTURE_2D, r.tex);
      gl.uniform1f(this.blitOpacity, r.opacity);
      drawQuad(gl);
    }
  };

  private reportStatus(id: string, status: LayerStatus): void {
    if (sameStatus(this.statuses.get(id), status)) return;
    this.statuses.set(id, status);
    this.onStatusChange?.(id, status);
  }
}
