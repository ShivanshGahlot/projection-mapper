/*{
  "DESCRIPTION": "A glowing outline around the edge of the layer with a clear inside, for tracing physical objects. Pulses gently and can send a bright chase light around the border.",
  "CREDIT": "Built-in",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Effect"],
  "INPUTS": [
    {"NAME": "color", "TYPE": "color", "DEFAULT": [0.2, 0.85, 1.0, 1.0], "LABEL": "Outline colour"},
    {"NAME": "thickness", "TYPE": "float", "DEFAULT": 0.008, "MIN": 0.002, "MAX": 0.05, "LABEL": "Line thickness"},
    {"NAME": "glow", "TYPE": "float", "DEFAULT": 0.03, "MIN": 0.0, "MAX": 0.15, "LABEL": "Glow size"},
    {"NAME": "inset", "TYPE": "float", "DEFAULT": 0.04, "MIN": 0.0, "MAX": 0.3, "LABEL": "Distance from edge"},
    {"NAME": "corner", "TYPE": "float", "DEFAULT": 0.05, "MIN": 0.0, "MAX": 0.3, "LABEL": "Corner roundness"},
    {"NAME": "pulse", "TYPE": "float", "DEFAULT": 0.4, "MIN": 0.0, "MAX": 1.0, "LABEL": "Pulse amount"},
    {"NAME": "chase", "TYPE": "bool", "DEFAULT": true, "LABEL": "Chasing light"},
    {"NAME": "speed", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 2.0, "LABEL": "Speed"}
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

void main() {
  float t = TIME * speed;
  vec2 p = centred();
  vec2 hs = vec2(aspect() * 0.5, 0.5) - inset;
  float cr = min(corner, min(hs.x, hs.y));
  vec2 q = abs(p) - (hs - cr);
  float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - cr;
  float dist = abs(d);
  float core = 1.0 - smoothstep(thickness * 0.5, thickness * 0.5 + 0.004, dist);
  float halo = exp(-dist / max(glow, 0.001)) * 0.7 * step(0.001, glow);
  float beat = 1.0 - pulse * (0.5 + 0.5 * sin(t * 3.0));
  float run = 1.0;
  if (chase) {
    float a = atan(p.y / hs.y, p.x / hs.x);
    run = 0.35 + 0.65 * pow(0.5 + 0.5 * cos(a - t * 1.5), 4.0);
  }
  float amount = clamp((core + halo) * beat * run, 0.0, 1.0);
  vec3 col = mix(color.rgb, vec3(1.0), core * 0.5);
  gl_FragColor = vec4(col * amount, amount);
}
