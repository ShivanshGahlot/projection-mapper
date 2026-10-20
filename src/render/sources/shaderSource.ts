// ISF shader content. Each layer owns its clock so pause / speed / offset are per layer.
// A broken shader only marks this layer as errored; it never throws out of render.

import { IsfProgram, type HostValues } from '../../isf/compile';
import { parseIsf } from '../../isf/parse';
import { RenderTarget } from '../../gl/renderTarget';
import { drawQuad } from '../../gl/quad';
import type { Content, ShaderContent } from '../../state/types';
import type { LayerClock, LayerStatus } from '../api';
import {
  STATUS_LOADING,
  STATUS_OK,
  errorStatus,
  newOutput,
  type ContentSource,
  type FrameInfo,
  type SizeRequest,
  type SourceOutput,
} from '../source';

/** Clock error beyond which a follower snaps to the master instead of slewing. */
const SNAP_S = 0.25;
const SLEW = 0.1;

export class ShaderSource implements ContentSource {
  readonly generated = true;
  readonly mirrorX = false;
  private content: ShaderContent;
  private program: IsfProgram | null = null;
  private readonly target: RenderTarget;
  private src: string | null = null;
  private parseError: LayerStatus | null = null;
  private renderError: LayerStatus | null = null;
  private clock = 0;
  private step = 0;
  /** Clock seconds per real second as of the last tick (0 when paused). */
  private rate = 0;
  private ref: { t: number; at: number; r: number } | null = null;
  private frameIndex = 0;
  private dirty = true;
  private rendered = false;
  private lastKey = '';
  private readonly out = newOutput();
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

  constructor(
    private readonly gl: WebGL2RenderingContext,
    content: ShaderContent,
  ) {
    this.target = new RenderTarget(gl);
    this.content = content;
    this.out.texture = this.target.texture;
    this.out.premultiplied = false;
    this.build(content.source);
  }

  private build(source: string): void {
    this.src = source;
    this.program?.dispose();
    this.program = null;
    this.parseError = null;
    this.renderError = null;
    this.frameIndex = 0;
    this.rendered = false;
    try {
      this.program = new IsfProgram(this.gl, parseIsf(source));
    } catch (e) {
      this.parseError = errorStatus(e);
    }
  }

  update(content: Content): boolean {
    if (content.type !== 'shader') return false;
    this.content = content;
    this.dirty = true;
    if (content.source !== this.src) this.build(content.source);
    return true;
  }

  tick(frame: FrameInfo): void {
    const c = this.content;
    this.rate = c.paused || frame.globalPaused ? 0 : frame.globalSpeed * c.speed;
    this.step = frame.dt * this.rate;
    this.clock += this.step;
    this.follow(frame.absMs);
    const d = this.host.date as [number, number, number, number];
    d[0] = frame.date[0];
    d[1] = frame.date[1];
    d[2] = frame.date[2];
    d[3] = frame.date[3];
    this.program?.poll();
  }

  getClock(): LayerClock {
    return { t: this.clock, r: this.rate };
  }

  syncClock(clock: LayerClock, atAbsMs: number): void {
    this.ref = clock.t === undefined ? null : { t: clock.t, at: atAbsMs, r: clock.r };
  }

  /** Steers the clock toward the master's: snap when far off or paused, else slew 10% of the error per frame. */
  private follow(absMs: number): void {
    const ref = this.ref;
    if (!ref) return;
    const age = absMs - ref.at;
    if (age > 5000) return; // master went quiet (hidden / closed): run free
    const err = ref.t + (age / 1000) * ref.r - this.clock;
    if (err === 0) return;
    if (ref.r === 0 || this.rate === 0 || Math.abs(err) > SNAP_S) this.clock += err;
    else this.clock += err * SLEW;
    this.dirty = true;
  }

  render(size: SizeRequest): SourceOutput | null {
    const program = this.program;
    if (!program || this.parseError || this.renderError) return null;
    try {
      if (program.poll() !== 'ready') return null;
      const { w, h, bw, bh } = size;
      const key = `${w}x${h}/${bw}x${bh}`;
      if (this.step === 0 && !this.dirty && this.rendered && key === this.lastKey) return this.out;
      this.target.ensureSize(bw, bh);
      this.target.bind();
      this.gl.viewport(0, 0, w, h);
      const host = this.host;
      host.time = this.clock + this.content.timeOffset;
      host.timeDelta = this.step;
      host.frameIndex = this.frameIndex++;
      host.renderWidth = w;
      host.renderHeight = h;
      program.use(host, this.content.values);
      drawQuad(this.gl);
      const tw = this.target.width;
      const th = this.target.height;
      const o = this.out;
      o.scaleX = w / tw;
      o.scaleY = h / th;
      o.minX = 0.5 / tw;
      o.minY = 0.5 / th;
      o.maxX = (w - 0.5) / tw;
      o.maxY = (h - 0.5) / th;
      this.dirty = false;
      this.rendered = true;
      this.lastKey = key;
      return o;
    } catch (e) {
      this.renderError = errorStatus(e);
      return null;
    }
  }

  nativeSize(): null {
    return null;
  }

  status(): LayerStatus {
    if (this.parseError) return this.parseError;
    if (this.renderError) return this.renderError;
    if (!this.program) return STATUS_LOADING;
    const s = this.program.poll();
    if (s === 'error') {
      const m = this.program.error;
      if (this.renderError === null && (this.errorCache?.message !== m)) this.errorCache = { state: 'error', message: m };
      return this.errorCache!;
    }
    return s === 'ready' ? STATUS_OK : STATUS_LOADING;
  }

  private errorCache: { state: 'error'; message: string } | null = null;

  dispose(): void {
    this.program?.dispose();
    this.target.dispose();
  }
}
