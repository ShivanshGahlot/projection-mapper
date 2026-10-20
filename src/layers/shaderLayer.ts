// A layer that runs an ISF shader into its own render target. A broken shader
// only marks this layer as errored; it never throws out of render.

import { IsfProgram, type HostValues } from '../isf/compile';
import { parseIsf } from '../isf/parse';
import { RenderTarget } from '../gl/renderTarget';
import { drawQuad } from '../gl/quad';
import type { LayerState } from '../state/types';
import type { FrameContext, LayerRuntime, LayerStatus } from './layer';

export class ShaderLayer implements LayerRuntime {
  private program: IsfProgram | null = null;
  private readonly target: RenderTarget;
  private state: LayerState;
  private source: string | null = null;
  private parseError: string | null = null;
  private renderError: string | null = null;
  private clock = 0;
  private timeDelta = 0;
  private frameIndex = 0;
  private readonly host: HostValues = {
    time: 0,
    timeDelta: 0,
    frameIndex: 0,
    renderWidth: 1,
    renderHeight: 1,
    date: [0, 0, 0, 0],
    audioBass: 0,
    audioMid: 0,
    audioHigh: 0,
    audioBeat: 0,
    audioLevel: 0,
  };

  constructor(private readonly gl: WebGL2RenderingContext, state: LayerState) {
    this.target = new RenderTarget(gl);
    this.state = state;
    this.update(state);
  }

  get status(): LayerStatus {
    if (this.parseError) return { state: 'error', message: this.parseError };
    if (this.renderError) return { state: 'error', message: this.renderError };
    if (!this.program) return { state: 'loading' };
    const s = this.program.poll();
    if (s === 'error') return { state: 'error', message: this.program.error };
    return s === 'ready' ? { state: 'ok' } : { state: 'loading' };
  }

  update(state: LayerState): void {
    this.state = state;
    if (state.source === this.source) return;
    this.source = state.source;
    this.program?.dispose();
    this.program = null;
    this.parseError = null;
    this.renderError = null;
    this.frameIndex = 0;
    try {
      this.program = new IsfProgram(this.gl, parseIsf(state.source));
    } catch (e) {
      this.parseError = e instanceof Error ? e.message : String(e);
    }
  }

  tick(dt: number): void {
    const step = this.state.paused ? 0 : dt * this.state.speed;
    this.timeDelta = step;
    this.clock += step;
  }

  render(ctx: FrameContext, width: number, height: number): WebGLTexture | null {
    const program = this.program;
    if (!program || this.parseError || this.renderError) return null;
    try {
      if (program.poll() !== 'ready') return null;
      this.target.ensureSize(width, height);
      this.target.bind();
      const h = this.host;
      h.time = this.clock + this.state.timeOffset;
      h.timeDelta = this.timeDelta;
      h.frameIndex = this.frameIndex++;
      h.renderWidth = this.target.width;
      h.renderHeight = this.target.height;
      h.date = ctx.date;
      h.audioBass = ctx.audio.bass;
      h.audioMid = ctx.audio.mid;
      h.audioHigh = ctx.audio.high;
      h.audioBeat = ctx.audio.beat;
      h.audioLevel = ctx.audio.level;
      program.use(h, this.state.values);
      drawQuad(this.gl);
      return this.target.texture;
    } catch (e) {
      this.renderError = e instanceof Error ? e.message : String(e);
      return null;
    }
  }

  dispose(): void {
    this.program?.dispose();
    this.target.dispose();
  }
}
