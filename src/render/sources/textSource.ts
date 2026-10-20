// Text content: laid out on an offscreen 2D canvas, re-drawn only when the text
// props or the (bucketed) size change, then uploaded as a texture.

import type { Content, TextContent } from '../../state/types';
import type { LayerStatus } from '../api';
import { STATUS_OK, makeTexture, newOutput, setFullOutput, type ContentSource, type FrameInfo, type SizeRequest, type SourceOutput } from '../source';

const GENERIC_FAMILIES = new Set(['serif', 'sans-serif', 'monospace', 'cursive', 'fantasy', 'system-ui', 'ui-monospace', 'ui-serif', 'ui-sans-serif']);

function fontString(t: TextContent): string {
  const fam = t.fontFamily.trim() || 'system-ui';
  const quoted = GENERIC_FAMILIES.has(fam) || /["',]/.test(fam) ? fam : `"${fam}"`;
  return `${t.italic ? 'italic ' : ''}${t.weight} ${t.fontSize}px ${quoted}`;
}

export class TextSource implements ContentSource {
  readonly generated = true;
  readonly mirrorX = false;
  private content: TextContent;
  private key = '';
  private dirty = true;
  private drawnKey = '';
  private readonly canvas = document.createElement('canvas');
  private readonly ctx = this.canvas.getContext('2d')!;
  private readonly tex: WebGLTexture;
  private readonly out = newOutput();
  private uploadedW = 0;
  private uploadedH = 0;

  constructor(
    private readonly gl: WebGL2RenderingContext,
    content: TextContent,
  ) {
    this.tex = makeTexture(gl);
    this.content = content;
    this.update(content);
  }

  update(content: Content): boolean {
    if (content.type !== 'text') return false;
    this.content = content;
    const key = JSON.stringify(content);
    if (key !== this.key) {
      this.key = key;
      this.dirty = true;
      // Re-draw once a web font that was not loaded yet arrives.
      const font = fontString(content);
      if (document.fonts && !document.fonts.check(font, content.text)) {
        document.fonts.load(font, content.text).then(
          () => {
            if (this.key === key) this.dirty = true;
          },
          () => {},
        );
      }
    }
    return true;
  }

  tick(_frame: FrameInfo): void {}

  render(size: SizeRequest): SourceOutput | null {
    const w = size.bw;
    const h = size.bh;
    const drawKey = `${this.key}|${w}x${h}|${size.refW.toFixed(1)}x${size.refH.toFixed(1)}`;
    if (this.dirty || drawKey !== this.drawnKey) {
      this.draw(w, h, size.refW, size.refH);
      const gl = this.gl;
      gl.bindTexture(gl.TEXTURE_2D, this.tex);
      if (w === this.uploadedW && h === this.uploadedH) {
        gl.texSubImage2D(gl.TEXTURE_2D, 0, 0, 0, gl.RGBA, gl.UNSIGNED_BYTE, this.canvas);
      } else {
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA8, gl.RGBA, gl.UNSIGNED_BYTE, this.canvas);
        this.uploadedW = w;
        this.uploadedH = h;
      }
      this.dirty = false;
      this.drawnKey = drawKey;
    }
    return setFullOutput(this.out, this.tex, true);
  }

  private draw(w: number, h: number, refW: number, refH: number): void {
    const c = this.content;
    const cv = this.canvas;
    const ctx = this.ctx;
    if (cv.width !== w) cv.width = w;
    if (cv.height !== h) cv.height = h;
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.clearRect(0, 0, w, h);
    ctx.setTransform(w / refW, 0, 0, h / refH, 0, 0);
    if (c.background) {
      ctx.fillStyle = c.background;
      ctx.fillRect(0, 0, refW, refH);
    }
    ctx.font = fontString(c);
    ctx.fillStyle = c.color;
    ctx.textBaseline = 'middle';
    const spacing = 'letterSpacing' in ctx;
    if (spacing) (ctx as unknown as { letterSpacing: string }).letterSpacing = `${c.letterSpacing}px`;

    const text = c.uppercase ? c.text.toUpperCase() : c.text;
    const lines: { text: string; last: boolean }[] = [];
    for (const para of text.split('\n')) {
      const words = para.split(/\s+/).filter(Boolean);
      if (words.length === 0) {
        lines.push({ text: '', last: true });
        continue;
      }
      let line = words[0];
      for (let i = 1; i < words.length; i++) {
        const next = `${line} ${words[i]}`;
        if (ctx.measureText(next).width > refW) {
          lines.push({ text: line, last: false });
          line = words[i];
        } else {
          line = next;
        }
      }
      lines.push({ text: line, last: true });
    }

    const lh = c.fontSize * c.lineHeight;
    let y = (refH - lines.length * lh) / 2 + lh / 2;
    for (const line of lines) {
      if (c.align === 'justify' && !line.last && line.text.includes(' ')) {
        this.drawJustified(ctx, line.text, y, refW);
      } else {
        const x = c.align === 'center' ? refW / 2 : c.align === 'right' ? refW : 0;
        ctx.textAlign = c.align === 'center' ? 'center' : c.align === 'right' ? 'right' : 'left';
        ctx.fillText(line.text, x, y);
      }
      y += lh;
    }
  }

  private drawJustified(ctx: CanvasRenderingContext2D, line: string, y: number, width: number): void {
    const words = line.split(' ');
    let total = 0;
    for (const word of words) total += ctx.measureText(word).width;
    const gap = (width - total) / (words.length - 1);
    ctx.textAlign = 'left';
    let x = 0;
    for (const word of words) {
      ctx.fillText(word, x, y);
      x += ctx.measureText(word).width + gap;
    }
  }

  nativeSize(): null {
    return null;
  }

  status(): LayerStatus {
    return STATUS_OK;
  }

  dispose(): void {
    this.gl.deleteTexture(this.tex);
  }
}
