// The projector test pattern: grid, crosshair, circle, centre lines and the
// resolution label, drawn once per size and uploaded as a texture.

import { makeTexture } from './source';

export class TestPattern {
  readonly texture: WebGLTexture;
  private readonly canvas = document.createElement('canvas');
  private key = '';

  constructor(private readonly gl: WebGL2RenderingContext) {
    this.texture = makeTexture(gl);
  }

  /** Re-draws when the render size or the project canvas size changed. Texture is premultiplied. */
  ensure(w: number, h: number, projW: number, projH: number): void {
    const key = `${w}x${h}|${projW}x${projH}`;
    if (key === this.key) return;
    this.key = key;
    const cv = this.canvas;
    cv.width = w;
    cv.height = h;
    const ctx = cv.getContext('2d')!;
    ctx.clearRect(0, 0, w, h);

    const cols = 16;
    const cell = w / cols;
    const line = Math.max(1, Math.round(Math.min(w, h) / 540));
    ctx.lineWidth = line;
    ctx.strokeStyle = 'rgba(255,255,255,0.55)';
    ctx.beginPath();
    for (let x = 0; x <= w + 0.5; x += cell) {
      ctx.moveTo(Math.round(x) + 0.5, 0);
      ctx.lineTo(Math.round(x) + 0.5, h);
    }
    for (let y = 0; y <= h + 0.5; y += cell) {
      ctx.moveTo(0, Math.round(y) + 0.5);
      ctx.lineTo(w, Math.round(y) + 0.5);
    }
    ctx.stroke();

    // Centre lines and crosshair.
    ctx.lineWidth = line * 2;
    ctx.strokeStyle = '#ff3b3b';
    ctx.beginPath();
    ctx.moveTo(w / 2, 0);
    ctx.lineTo(w / 2, h);
    ctx.moveTo(0, h / 2);
    ctx.lineTo(w, h / 2);
    ctx.stroke();
    ctx.strokeStyle = '#ffffff';
    const arm = Math.min(w, h) * 0.04;
    ctx.beginPath();
    ctx.moveTo(w / 2 - arm, h / 2);
    ctx.lineTo(w / 2 + arm, h / 2);
    ctx.moveTo(w / 2, h / 2 - arm);
    ctx.lineTo(w / 2, h / 2 + arm);
    ctx.stroke();

    // Circle (should look round on a correctly set up projector) and the outer border.
    ctx.strokeStyle = '#3bff8a';
    ctx.beginPath();
    ctx.arc(w / 2, h / 2, Math.min(w, h) * 0.45, 0, Math.PI * 2);
    ctx.stroke();
    ctx.strokeStyle = '#ffffff';
    ctx.lineWidth = line * 4;
    ctx.strokeRect(line * 2, line * 2, w - line * 4, h - line * 4);

    // Label.
    const fs = Math.max(12, Math.round(h * 0.045));
    ctx.font = `600 ${fs}px system-ui, sans-serif`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    const label = `${projW} × ${projH}`;
    const tw = ctx.measureText(label).width;
    ctx.fillStyle = 'rgba(0,0,0,0.75)';
    ctx.fillRect(w / 2 - tw / 2 - fs * 0.6, h / 2 + arm * 1.4, tw + fs * 1.2, fs * 1.6);
    ctx.fillStyle = '#ffffff';
    ctx.fillText(label, w / 2, h / 2 + arm * 1.4 + fs * 0.8);

    const gl = this.gl;
    gl.bindTexture(gl.TEXTURE_2D, this.texture);
    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA8, gl.RGBA, gl.UNSIGNED_BYTE, cv);
  }

  dispose(): void {
    this.gl.deleteTexture(this.texture);
  }
}
