// GLSL ES 3.00 programs used by the compositor, and their uniform lookups.

import { createProgram } from '../gl/program';

/** Full-screen quad on attribute 0 (see gl/quad.ts). v_uv is y-up. */
export const FULLSCREEN_VS = `#version 300 es
in vec2 a_pos;
out vec2 v_uv;
void main() {
  v_uv = a_pos * 0.5 + 0.5;
  gl_Position = vec4(a_pos, 0.0, 1.0);
}`;

// The layer quad: 4 corners in clip space, triangle-strip order TL, TR, BL, BR.
// No varyings: the fragment shader derives content UV from the pixel position
// with the inverse homography, so there is no seam across the two triangles.
const LAYER_VS = `#version 300 es
uniform vec2 u_c[4];
void main() {
  gl_Position = vec4(u_c[gl_VertexID], 0.0, 1.0);
}`;

const LAYER_FS = `#version 300 es
precision highp float;
uniform mat3 u_inv;       // output px (y down) -> unit square
uniform vec2 u_size;      // render size in px
uniform sampler2D u_tex;
uniform vec4 u_crop;      // x, y, w, h; y down
uniform vec2 u_texScale;
uniform vec4 u_texRect;
uniform float u_premul;
uniform float u_flipX;
uniform float u_ellipse;
uniform vec2 u_quadSize;  // average side lengths in px
uniform float u_radius;   // px
uniform float u_feather;  // px
uniform float u_opacity;
uniform int u_maskCount;
uniform vec4 u_maskOpacity;
uniform sampler2D u_m0;
uniform sampler2D u_m1;
uniform sampler2D u_m2;
uniform sampler2D u_m3;
out vec4 o;

void main() {
  vec2 frag = gl_FragCoord.xy;
  vec3 q = u_inv * vec3(frag.x, u_size.y - frag.y, 1.0);
  vec2 uv = q.xy / q.z;

  // Signed distance (px, negative inside) to the shape, measured in unit-square space scaled by the quad size.
  vec2 p = (uv - 0.5) * u_quadSize;
  vec2 hs = 0.5 * u_quadSize;
  float d;
  if (u_ellipse > 0.5) {
    vec2 k = p / hs;
    float k0 = length(k);
    float k1 = length(p / (hs * hs));
    d = k1 > 1e-6 ? k0 * (k0 - 1.0) / k1 : -min(hs.x, hs.y);
  } else {
    float r = min(u_radius, min(hs.x, hs.y));
    vec2 e = abs(p) - hs + r;
    d = length(max(e, 0.0)) + min(max(e.x, e.y), 0.0) - r;
  }
  float cov = u_feather > 0.0 ? clamp(-d / u_feather, 0.0, 1.0) : clamp(0.5 - d, 0.0, 1.0);

  if (u_flipX > 0.5) uv.x = 1.0 - uv.x;
  vec2 c = u_crop.xy + uv * u_crop.zw;
  vec2 t = clamp(vec2(c.x, 1.0 - c.y) * u_texScale, u_texRect.xy, u_texRect.zw);
  vec4 col = texture(u_tex, t);
  col.rgb *= mix(col.a, 1.0, u_premul);
  col *= cov * u_opacity;

  if (u_maskCount > 0) {
    vec2 muv = frag / u_size;
    col *= mix(1.0, texture(u_m0, muv).r, u_maskOpacity.x);
    if (u_maskCount > 1) col *= mix(1.0, texture(u_m1, muv).r, u_maskOpacity.y);
    if (u_maskCount > 2) col *= mix(1.0, texture(u_m2, muv).r, u_maskOpacity.z);
    if (u_maskCount > 3) col *= mix(1.0, texture(u_m3, muv).r, u_maskOpacity.w);
  }
  o = col;
}`;

// Cuts everything composited so far to the background colour where the mask is 0.
const MASK_FS = `#version 300 es
precision highp float;
in vec2 v_uv;
uniform sampler2D u_mask;
uniform vec3 u_bg;
uniform float u_opacity;
out vec4 o;
void main() {
  float a = u_opacity * (1.0 - texture(u_mask, v_uv).r);
  o = vec4(u_bg * a, a);
}`;

// Draws a texture region with optional checkerboard behind it and a brightness factor.
const BLIT_FS = `#version 300 es
precision highp float;
in vec2 v_uv;
uniform sampler2D u_tex;
uniform vec2 u_texScale;
uniform vec4 u_texRect;
uniform float u_premul;
uniform float u_checker;
uniform float u_useTex;
uniform float u_brightness;
out vec4 o;
void main() {
  vec4 c = vec4(0.0);
  if (u_useTex > 0.5) {
    c = texture(u_tex, clamp(v_uv * u_texScale, u_texRect.xy, u_texRect.zw));
    c.rgb *= mix(c.a, 1.0, u_premul);
  }
  if (u_checker > 0.5) {
    vec2 g = floor(gl_FragCoord.xy / 12.0);
    float s = mod(g.x + g.y, 2.0);
    c.rgb += vec3(mix(0.16, 0.24, s)) * (1.0 - c.a);
    c.a = 1.0;
  }
  c.rgb *= u_brightness;
  o = c;
}`;

type Locs<K extends string> = Record<K, WebGLUniformLocation | null>;

function locate<K extends string>(gl: WebGL2RenderingContext, program: WebGLProgram, names: readonly K[]): Locs<K> {
  const out = {} as Locs<K>;
  for (const n of names) out[n] = gl.getUniformLocation(program, n);
  return out;
}

const LAYER_UNIFORMS = [
  'u_c', 'u_inv', 'u_size', 'u_tex', 'u_crop', 'u_texScale', 'u_texRect', 'u_premul', 'u_flipX', 'u_ellipse',
  'u_quadSize', 'u_radius', 'u_feather', 'u_opacity', 'u_maskCount', 'u_maskOpacity', 'u_m0', 'u_m1', 'u_m2', 'u_m3',
] as const;
const MASK_UNIFORMS = ['u_mask', 'u_bg', 'u_opacity'] as const;
const BLIT_UNIFORMS = ['u_tex', 'u_texScale', 'u_texRect', 'u_premul', 'u_checker', 'u_useTex', 'u_brightness'] as const;

export interface Programs {
  layer: { program: WebGLProgram; u: Locs<(typeof LAYER_UNIFORMS)[number]> };
  mask: { program: WebGLProgram; u: Locs<(typeof MASK_UNIFORMS)[number]> };
  blit: { program: WebGLProgram; u: Locs<(typeof BLIT_UNIFORMS)[number]> };
}

export function createPrograms(gl: WebGL2RenderingContext): Programs {
  const layer = createProgram(gl, LAYER_VS, LAYER_FS);
  const mask = createProgram(gl, FULLSCREEN_VS, MASK_FS);
  const blit = createProgram(gl, FULLSCREEN_VS, BLIT_FS);
  gl.useProgram(layer);
  const lu = locate(gl, layer, LAYER_UNIFORMS);
  gl.uniform1i(lu.u_tex, 0);
  gl.uniform1i(lu.u_m0, 1);
  gl.uniform1i(lu.u_m1, 2);
  gl.uniform1i(lu.u_m2, 3);
  gl.uniform1i(lu.u_m3, 4);
  gl.useProgram(mask);
  const mu = locate(gl, mask, MASK_UNIFORMS);
  gl.uniform1i(mu.u_mask, 0);
  gl.useProgram(blit);
  const bu = locate(gl, blit, BLIT_UNIFORMS);
  gl.uniform1i(bu.u_tex, 0);
  return {
    layer: { program: layer, u: lu },
    mask: { program: mask, u: mu },
    blit: { program: blit, u: bu },
  };
}
