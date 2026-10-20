// Uploads an HTMLVideoElement to a texture, only when a new frame is available.

import { makeTexture } from '../source';

export class VideoFeed {
  readonly texture: WebGLTexture;
  width = 0;
  height = 0;
  hasFrame = false;
  private fresh = false;
  private lastTime = -1;
  private disposed = false;
  private readonly hasRvfc: boolean;

  constructor(
    private readonly gl: WebGL2RenderingContext,
    readonly video: HTMLVideoElement,
  ) {
    this.texture = makeTexture(gl);
    this.hasRvfc = 'requestVideoFrameCallback' in video;
    if (this.hasRvfc) this.arm();
  }

  private arm(): void {
    this.video.requestVideoFrameCallback(() => {
      if (this.disposed) return;
      this.fresh = true;
      this.arm();
    });
  }

  /** Uploads the current frame if it is new. Returns true once any frame has been uploaded. */
  update(): boolean {
    const v = this.video;
    if (v.readyState < 2 || v.videoWidth === 0) return this.hasFrame;
    if (this.hasFrame) {
      if (this.hasRvfc) {
        if (!this.fresh) return true;
      } else if (v.currentTime === this.lastTime) {
        return true;
      }
    }
    this.fresh = false;
    this.lastTime = v.currentTime;
    const gl = this.gl;
    gl.bindTexture(gl.TEXTURE_2D, this.texture);
    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA8, gl.RGBA, gl.UNSIGNED_BYTE, v);
    this.width = v.videoWidth;
    this.height = v.videoHeight;
    this.hasFrame = true;
    return true;
  }

  dispose(): void {
    this.disposed = true;
    this.gl.deleteTexture(this.texture);
  }
}
