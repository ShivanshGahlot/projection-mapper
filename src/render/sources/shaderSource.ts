// ISF shader content. Each layer owns its clock so pause / speed / offset are per layer.
// A broken shader only marks this layer as errored; it never throws out of render.

import { IsfProgram, type HostValues } from '../../isf/compile';
import { parseIsf } from '../../isf/parse';
import { RenderTarget } from '../../gl/renderTarget';
import { drawQuad } from '../../gl/quad';
import type { Content, ShaderContent } from '../../state/types';
import type { LayerStatus } from '../api';
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
    this.step = c.paused || frame.globalPaused ? 0 : frame.dt * frame.globalSpeed * c.speed;
    this.clock += this.step;
    const d = this.host.date as [number, number, number, number];
    d[0] = frame.date[0];
    d[1] = frame.date[1];
    d[2] = frame.date[2];
    d[3] = frame.date[3];
    this.program?.poll();
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
