// The alignment grid shown on new surfaces and on shader layers with no shader
// chosen yet: lines only (premultiplied, transparent between them), square cells,
// outline, centre cross and an inscribed circle. Line widths are in content pixels.

import { createProgram } from '../gl/program';
import { drawQuad } from '../gl/quad';
import type { RenderTarget } from '../gl/renderTarget';
import { FULLSCREEN_VS } from './programs';

const GRID_FS = `#version 300 es
precision highp float;
in vec2 v_uv;
uniform vec2 u_size;
out vec4 o;
float line(float d, float w) {
  return 1.0 - smoothstep(w * 0.5, w * 0.5 + 1.0, d);
}
void main() {
  vec2 p = v_uv * u_size;
  float px = max(1.0, min(u_size.x, u_size.y) / 360.0);
  float cell = max(u_size.x, u_size.y) / 10.0;
  vec2 c = p - u_size * 0.5;
  vec2 g = abs(mod(c + cell * 0.5, cell) - cell * 0.5);
  float grid = line(min(g.x, g.y), px);
  vec2 e = min(p, u_size - p);
  float border = line(min(e.x, e.y), px * 6.0);
  float cross = max(line(abs(c.x), px * 2.0), line(abs(c.y), px * 2.0));
  float circle = line(abs(length(c) - min(u_size.x, u_size.y) * 0.45), px * 2.0);
  vec4 c4 = vec4(vec3(0.75, 0.75, 0.8), 1.0) * grid * 0.8;
  float a = max(cross, circle);
  c4 = vec4(0.23, 0.51, 0.96, 1.0) * a + c4 * (1.0 - a);
  c4 = vec4(1.0) * border + c4 * (1.0 - border);
  o = c4;
}`;

const programs = new WeakMap<WebGL2RenderingContext, { program: WebGLProgram; size: WebGLUniformLocation }>();

/** Draws the grid into the whole of `target` (already sized). */
export function drawGrid(gl: WebGL2RenderingContext, target: RenderTarget): void {
  let g = programs.get(gl);
  if (!g) {
    const program = createProgram(gl, FULLSCREEN_VS, GRID_FS);
    g = { program, size: gl.getUniformLocation(program, 'u_size')! };
    programs.set(gl, g);
  }
  target.bind();
  gl.useProgram(g.program);
  gl.uniform2f(g.size, target.width, target.height);
  drawQuad(gl);
}
