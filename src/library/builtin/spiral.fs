/*{
  "DESCRIPTION": "Crisp spiral bands winding in or out of the centre, hypnotically.",
  "CREDIT": "Built-in",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Motion"],
  "INPUTS": [
    {"NAME": "lineColor", "TYPE": "color", "DEFAULT": [1.0, 0.85, 0.35, 1.0], "LABEL": "Spiral colour"},
    {"NAME": "backColor", "TYPE": "color", "DEFAULT": [0.08, 0.04, 0.25, 1.0], "LABEL": "Background colour"},
    {"NAME": "arms", "TYPE": "float", "DEFAULT": 2.0, "MIN": 1.0, "MAX": 8.0, "LABEL": "Number of arms"},
    {"NAME": "tightness", "TYPE": "float", "DEFAULT": 5.0, "MIN": 1.0, "MAX": 20.0, "LABEL": "Tightness"},
    {"NAME": "thickness", "TYPE": "float", "DEFAULT": 0.45, "MIN": 0.1, "MAX": 0.9, "LABEL": "Band thickness"},
    {"NAME": "softness", "TYPE": "float", "DEFAULT": 0.08, "MIN": 0.01, "MAX": 0.4, "LABEL": "Edge softness"},
    {"NAME": "speed", "TYPE": "float", "DEFAULT": 0.6, "MIN": -2.0, "MAX": 2.0, "LABEL": "Speed (negative reverses)"}
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
  vec2 p = centred();
  float r = length(p);
  float a = atan(p.y, p.x);
  float k = floor(arms + 0.5);
  float w = fract(r * tightness - a / TAU * k - TIME * speed * 0.5);
  float bar = smoothstep(0.0, softness, w) * (1.0 - smoothstep(thickness, thickness + softness, w));
  vec3 col = mix(backColor.rgb, lineColor.rgb, bar);
  col *= 1.1 - 0.5 * r;
  gl_FragColor = vec4(clamp(col, 0.0, 1.0), 1.0);
}
