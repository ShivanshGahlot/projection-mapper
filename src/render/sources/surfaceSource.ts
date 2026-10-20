// Surface content: nothing, a flat colour, a gradient (rendered by a small shader),
// an image or a looping video.

import { createProgram } from '../../gl/program';
import { drawQuad } from '../../gl/quad';
import { RenderTarget } from '../../gl/renderTarget';
import type { Content, Fill, SurfaceContent } from '../../state/types';
import type { LayerClock, LayerStatus } from '../api';
import { drawGrid } from '../grid';
import { FULLSCREEN_VS } from '../programs';
import {
  STATUS_LOADING,
  STATUS_OK,
  errorStatus,
  makeTexture,
  newOutput,
  parseColor,
  setFullOutput,
  type ContentSource,
  type FrameInfo,
  type SizeRequest,
  type SourceEnv,
  type SourceOutput,
} from '../source';
import { VideoFeed } from './videoFeed';

const MAX_STOPS = 16;

const GRADIENT_FS = `#version 300 es
precision highp float;
in vec2 v_uv;
uniform int u_radial;
uniform float u_angle;
uniform int u_n;
uniform float u_pos[${MAX_STOPS}];
uniform vec3 u_col[${MAX_STOPS}];
out vec4 o;
void main() {
  vec2 p = v_uv - 0.5;
  float t;
  if (u_radial == 1) {
    t = length(p) * 2.0;
  } else {
    // CSS convention: 0deg points up, 90deg points right (v_uv is y-up).
    vec2 d = vec2(sin(u_angle), cos(u_angle));
    t = dot(p, d) / (abs(d.x) + abs(d.y)) + 0.5;
  }
  vec3 c = u_col[0];
  for (int i = 1; i < ${MAX_STOPS}; i++) {
    if (i >= u_n) break;
    float f = clamp((t - u_pos[i - 1]) / max(u_pos[i] - u_pos[i - 1], 1e-5), 0.0, 1.0);
    c = mix(c, u_col[i], f);
  }
  o = vec4(c, 1.0);
}`;

interface GradientGl {
  program: WebGLProgram;
  radial: WebGLUniformLocation;
  angle: WebGLUniformLocation;
  n: WebGLUniformLocation;
  pos: WebGLUniformLocation;
  col: WebGLUniformLocation;
}
const gradientPrograms = new WeakMap<WebGL2RenderingContext, GradientGl>();
function gradientProgram(gl: WebGL2RenderingContext): GradientGl {
  let g = gradientPrograms.get(gl);
  if (!g) {
    const program = createProgram(gl, FULLSCREEN_VS, GRADIENT_FS);
    const u = (n: string) => gl.getUniformLocation(program, n)!;
    g = { program, radial: u('u_radial'), angle: u('u_angle'), n: u('u_n'), pos: u('u_pos[0]'), col: u('u_col[0]') };
    gradientPrograms.set(gl, g);
  }
  return g;
}

export class SurfaceSource implements ContentSource {
  private fill: Fill = { type: 'none' };
  private key = '';
  private tex: WebGLTexture | null = null;
  private feed: VideoFeed | null = null;
  private target: RenderTarget | null = null;
  private image: HTMLImageElement | null = null;
  private imageReady = false;
  private imageSize: { width: number; height: number } | null = null;
  private loop = true;
  private rate = 1;
  private wantPlaying = true;
  private paused = false;
  private ref: { v: number; at: number; r: number } | null = null;
  private nudged = false;
  private st: LayerStatus = STATUS_OK;
  private token = 0;
  private gradKey = '';
  private readonly stopPos = new Float32Array(MAX_STOPS);
  private readonly stopCol = new Float32Array(MAX_STOPS * 3);
  private readonly out = newOutput();

  constructor(
    private readonly env: SourceEnv,
    content: SurfaceContent,
  ) {
    this.setFill(content.fill);
  }

  get generated(): boolean {
    return this.fill.type === 'gradient' || this.fill.type === 'grid';
  }
  readonly mirrorX = false;

  update(content: Content): boolean {
    if (content.type !== 'surface') return false;
    const fill = content.fill;
    const key = JSON.stringify(fill);
    if (key === this.key) return true;
    const cur = this.fill;
    if (fill.type === 'video' && cur.type === 'video' && fill.assetId === cur.assetId) {
      // Loop/speed changes apply to the playing video (in tick) instead of reloading it.
      this.fill = fill;
      this.key = key;
      this.loop = fill.loop;
      this.rate = fill.playbackRate;
      return true;
    }
    this.setFill(fill);
    return true;
  }

  private release(): void {
    const gl = this.env.gl;
    this.token++;
    this.nudged = false;
    this.ref = null;
    if (this.tex) gl.deleteTexture(this.tex);
    this.tex = null;
    this.feed?.video.pause();
    this.feed?.video.removeAttribute('src');
    this.feed?.video.load();
    this.feed?.dispose();
    this.feed = null;
    this.target?.dispose();
    this.target = null;
    if (this.image) this.image.src = '';
    this.image = null;
    this.imageReady = false;
    this.imageSize = null;
    this.gradKey = '';
    this.st = STATUS_OK;
  }

  private setFill(fill: Fill): void {
    this.release();
    this.fill = fill;
    this.key = JSON.stringify(fill);
    const gl = this.env.gl;
    switch (fill.type) {
      case 'none':
        break;
      case 'color': {
        const [r, g, b] = parseColor(fill.color);
        this.tex = makeTexture(gl);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA8, 1, 1, 0, gl.RGBA, gl.UNSIGNED_BYTE, new Uint8Array([r * 255, g * 255, b * 255, 255]));
        break;
      }
      case 'gradient':
      case 'grid':
        this.target = new RenderTarget(gl);
        break;
      case 'image':
        this.loadImage(fill.assetId);
        break;
      case 'video':
        this.loadVideo(fill);
        break;
    }
  }

  private loadImage(assetId: string): void {
    const token = this.token;
    this.st = STATUS_LOADING;
    this.env.assets.getAssetUrl(assetId).then(
      (url) => {
        if (token !== this.token) return;
        if (!url) {
          this.st = { state: 'error', message: 'Image file is missing.' };
          return;
        }
        const img = new Image();
        img.onload = () => {
          if (token !== this.token) return;
          const gl = this.env.gl;
          this.tex = makeTexture(gl, true);
          gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA8, gl.RGBA, gl.UNSIGNED_BYTE, img);
          gl.generateMipmap(gl.TEXTURE_2D);
          this.imageSize = { width: img.naturalWidth, height: img.naturalHeight };
          this.imageReady = true;
          this.st = STATUS_OK;
        };
        img.onerror = () => {
          if (token === this.token) this.st = { state: 'error', message: 'Image could not be loaded.' };
        };
        img.src = url;
        this.image = img;
      },
      (e) => {
        if (token === this.token) this.st = errorStatus(e);
      },
    );
  }

  private loadVideo(fill: Extract<Fill, { type: 'video' }>): void {
    const token = this.token;
    this.st = STATUS_LOADING;
    this.loop = fill.loop;
    this.rate = fill.playbackRate;
    const video = document.createElement('video');
    video.muted = true;
    video.loop = fill.loop;
    video.playsInline = true;
    video.autoplay = true;
    video.playbackRate = fill.playbackRate;
    video.onerror = () => {
      if (token === this.token) this.st = { state: 'error', message: 'Video could not be loaded.' };
    };
    this.feed = new VideoFeed(this.env.gl, video);
    this.env.assets.getAssetUrl(fill.assetId).then(
      (url) => {
        if (token !== this.token) return;
        if (!url) {
          this.st = { state: 'error', message: 'Video file is missing.' };
          return;
        }
        video.src = url;
        if (this.wantPlaying) video.play().catch(() => {});
      },
      (e) => {
        if (token === this.token) this.st = errorStatus(e);
      },
    );
  }

  tick(frame: FrameInfo): void {
    const v = this.feed?.video;
    if (!v) return;
    this.wantPlaying = !frame.globalPaused;
    this.paused = frame.globalPaused;
    if (this.fill.type === 'video') {
      if (v.loop !== this.loop) v.loop = this.loop;
      if (!this.nudged && v.playbackRate !== this.rate) v.playbackRate = this.rate;
      this.follow(v, frame.absMs);
    }
    if (frame.globalPaused) {
      if (!v.paused) v.pause();
    } else if (v.paused && v.src) {
      v.play().catch(() => {});
    }
  }

  getClock(): LayerClock | null {
    const v = this.feed?.video;
    if (this.fill.type !== 'video' || !v || v.readyState < 1) return null;
    return { v: v.currentTime, r: this.paused ? 0 : this.rate };
  }

  syncClock(clock: LayerClock, atAbsMs: number): void {
    this.ref = clock.v === undefined ? null : { v: clock.v, at: atAbsMs, r: clock.r };
  }

  /**
   * Keeps the video within a frame or so of the master: seek when more than 150 ms off,
   * otherwise nudge playbackRate by +-3% while the error is above 30 ms.
   */
  private follow(v: HTMLVideoElement, absMs: number): void {
    const ref = this.ref;
    const dur = v.duration;
    if (!ref || v.readyState < 1 || !Number.isFinite(dur) || dur <= 0 || v.seeking) return;
    const age = absMs - ref.at;
    if (age > 5000) return;
    let target = ref.v + (age / 1000) * ref.r;
    let err = target - v.currentTime;
    if (this.loop) {
      target = ((target % dur) + dur) % dur;
      err = ((((target - v.currentTime) % dur) + dur + dur / 2) % dur) - dur / 2;
    } else {
      target = Math.min(target, dur);
      err = target - v.currentTime;
    }
    const abs = Math.abs(err);
    if (abs > (this.paused ? 0.04 : 0.15)) {
      v.currentTime = Math.min(Math.max(0, v.currentTime + err), dur);
      v.playbackRate = this.rate;
      this.nudged = false;
    } else if (!this.paused && abs > 0.03) {
      v.playbackRate = Math.max(0.0625, this.rate * (err > 0 ? 1.03 : 0.97));
      this.nudged = true;
    } else if (this.nudged) {
      v.playbackRate = this.rate;
      this.nudged = false;
    }
  }

  render(size: SizeRequest): SourceOutput | null {
    const gl = this.env.gl;
    const fill = this.fill;
    switch (fill.type) {
      case 'none':
        return null;
      case 'color':
        return this.tex ? setFullOutput(this.out, this.tex, true) : null;
      case 'image':
        return this.imageReady && this.tex ? setFullOutput(this.out, this.tex, true) : null;
      case 'video': {
        const feed = this.feed;
        if (!feed || !feed.update()) return null;
        if (this.st !== STATUS_OK) this.st = STATUS_OK;
        return setFullOutput(this.out, feed.texture, true);
      }
      case 'grid': {
        const target = this.target!;
        const key = `grid|${size.bw}x${size.bh}`;
        if (key !== this.gradKey) {
          target.ensureSize(size.bw, size.bh);
          drawGrid(gl, target);
          this.gradKey = key;
        }
        return setFullOutput(this.out, target.texture, true);
      }
      case 'gradient': {
        const target = this.target!;
        const key = `${fill.kind}|${fill.angle}|${JSON.stringify(fill.stops)}|${size.bw}x${size.bh}`;
        if (key !== this.gradKey) {
          const g = gradientProgram(gl);
          const stops = fill.stops.length ? [...fill.stops].sort((a, b) => a.pos - b.pos).slice(0, MAX_STOPS) : [{ pos: 0, color: '#000000' }];
          const n = stops.length;
          for (let i = 0; i < MAX_STOPS; i++) {
            const s = stops[Math.min(i, n - 1)];
            this.stopPos[i] = s.pos;
            this.stopCol.set(parseColor(s.color), i * 3);
          }
          target.ensureSize(size.bw, size.bh);
          target.bind();
          gl.useProgram(g.program);
          gl.uniform1i(g.radial, fill.kind === 'radial' ? 1 : 0);
          gl.uniform1f(g.angle, (fill.angle * Math.PI) / 180);
          gl.uniform1i(g.n, n);
          gl.uniform1fv(g.pos, this.stopPos);
          gl.uniform3fv(g.col, this.stopCol);
          drawQuad(gl);
          this.gradKey = key;
        }
        return setFullOutput(this.out, target.texture, true);
      }
    }
  }

  nativeSize(): { width: number; height: number } | null {
    if (this.fill.type === 'image') return this.imageSize;
    if (this.fill.type === 'video' && this.feed?.hasFrame) return { width: this.feed.width, height: this.feed.height };
    return null;
  }

  status(): LayerStatus {
    return this.st;
  }

  dispose(): void {
    this.release();
  }
}
