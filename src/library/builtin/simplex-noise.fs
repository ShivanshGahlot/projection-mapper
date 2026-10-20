/*{
  "DESCRIPTION": "Smooth, flowing simplex noise blending between two colours, with an optional finer detail layer.",
  "CREDIT": "Built-in",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Noise"],
  "INPUTS": [
    {"NAME": "colorA", "TYPE": "color", "DEFAULT": [0.04, 0.1, 0.22, 1.0], "LABEL": "Dark colour"},
    {"NAME": "colorB", "TYPE": "color", "DEFAULT": [0.3, 0.9, 0.85, 1.0], "LABEL": "Light colour"},
    {"NAME": "scale", "TYPE": "float", "DEFAULT": 2.2, "MIN": 0.5, "MAX": 8.0, "LABEL": "Scale"},
    {"NAME": "detail", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0, "LABEL": "Fine detail"},
    {"NAME": "contrast", "TYPE": "float", "DEFAULT": 1.3, "MIN": 0.5, "MAX": 3.0, "LABEL": "Contrast"},
    {"NAME": "speed", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 2.0, "LABEL": "Speed"}
  ]
}*/

#ifdef GL_ES
precision highp float;
#endif

const float PI = 3.14159265359;
const float TAU = 6.28318530718;

float hash21(vec2 p) {
  vec3 q = fract(vec3(p.xyx) * vec3(0.1031, 0.1030, 0.0973));
  q += dot(q, q.yzx + 33.33);
  return fract((q.x + q.y) * q.z);
}

vec2 grad2(vec2 i) {
  float a = hash21(i) * TAU;
  return vec2(cos(a), sin(a));
}

// Smooth value noise, 0..1.
float vnoise(vec2 p) {
  vec2 i = floor(p);
  vec2 f = fract(p);
  f = f * f * (3.0 - 2.0 * f);
  return mix(mix(hash21(i), hash21(i + vec2(1.0, 0.0)), f.x),
             mix(hash21(i + vec2(0.0, 1.0)), hash21(i + vec2(1.0, 1.0)), f.x), f.y);
}

// Gradient noise, roughly -1..1.
float gnoise(vec2 p) {
  vec2 i = floor(p);
  vec2 f = fract(p);
  vec2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
  float a = dot(grad2(i), f);
  float b = dot(grad2(i + vec2(1.0, 0.0)), f - vec2(1.0, 0.0));
  float c = dot(grad2(i + vec2(0.0, 1.0)), f - vec2(0.0, 1.0));
  float d = dot(grad2(i + vec2(1.0, 1.0)), f - vec2(1.0, 1.0));
  return mix(mix(a, b, u.x), mix(c, d, u.x), u.y) * 1.4;
}

mat2 rot(float a) {
  float c = cos(a), s = sin(a);
  return mat2(c, -s, s, c);
}

// Fractal sum of gradient noise, roughly 0..1.
float gfbm(vec2 p) {
  float s = 0.0;
  float a = 0.5;
  for (int i = 0; i < 4; i++) {
    s += a * gnoise(p);
    p = rot(0.5) * p * 2.02 + vec2(11.7, 3.1);
    a *= 0.5;
  }
  return 0.5 + 0.5 * s / 0.9375;
}

vec3 ramp3(vec3 a, vec3 b, vec3 c, float k) {
  k = clamp(k, 0.0, 1.0);
  return k < 0.5 ? mix(a, b, smoothstep(0.0, 1.0, k * 2.0)) : mix(b, c, smoothstep(0.0, 1.0, k * 2.0 - 1.0));
}

// Same as ramp3 but wraps back to the first colour, for endless cycling.
vec3 cycle3(vec3 a, vec3 b, vec3 c, float k) {
  k = fract(k) * 3.0;
  if (k < 1.0) return mix(a, b, smoothstep(0.0, 1.0, k));
  if (k < 2.0) return mix(b, c, smoothstep(0.0, 1.0, k - 1.0));
  return mix(c, a, smoothstep(0.0, 1.0, k - 2.0));
}

// Centre-origin coordinates where y spans -0.5..0.5 and x spans -aspect/2..aspect/2.
vec2 centred() {
  return (gl_FragCoord.xy - 0.5 * RENDERSIZE.xy) / RENDERSIZE.y;
}

float aspect() {
  return RENDERSIZE.x / RENDERSIZE.y;
}

// Simplex noise on a skewed triangular grid, roughly -1..1.
float snoise(vec2 p) {
  const float F = 0.36602540378;
  const float G = 0.2113248654;
  vec2 i = floor(p + (p.x + p.y) * F);
  vec2 x0 = p - i + (i.x + i.y) * G;
  vec2 o = x0.x > x0.y ? vec2(1.0, 0.0) : vec2(0.0, 1.0);
  vec2 x1 = x0 - o + G;
  vec2 x2 = x0 - 1.0 + 2.0 * G;
  vec3 h = max(0.5 - vec3(dot(x0, x0), dot(x1, x1), dot(x2, x2)), 0.0);
  h = h * h * h * h;
  vec3 n = h * vec3(dot(grad2(i), x0), dot(grad2(i + o), x1), dot(grad2(i + 1.0), x2));
  return 70.0 * (n.x + n.y + n.z);
}

void main() {
  float t = TIME * speed * 0.3;
  vec2 p = centred() * scale;
  float n = snoise(rot(t * 0.15) * p + vec2(t, t * 0.6));
  n += detail * 0.5 * snoise(rot(-t * 0.2) * p * 2.3 + vec2(-t * 1.3, 4.0 + t * 0.4));
  float v = 0.5 + 0.5 * n / (1.0 + 0.5 * detail);
  v = clamp((v - 0.5) * contrast + 0.5, 0.0, 1.0);
  gl_FragColor = vec4(mix(colorA.rgb, colorB.rgb, v), 1.0);
}
