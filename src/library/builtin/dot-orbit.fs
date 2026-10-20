/*{
  "DESCRIPTION": "Concentric rings of glowing dots, each ring orbiting in the opposite direction to its neighbour.",
  "CREDIT": "Built-in",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Pattern"],
  "INPUTS": [
    {"NAME": "dotColor", "TYPE": "color", "DEFAULT": [0.4, 0.9, 1.0, 1.0], "LABEL": "Inner dot colour"},
    {"NAME": "accentColor", "TYPE": "color", "DEFAULT": [1.0, 0.4, 0.7, 1.0], "LABEL": "Outer dot colour"},
    {"NAME": "backColor", "TYPE": "color", "DEFAULT": [0.02, 0.03, 0.1, 1.0], "LABEL": "Background colour"},
    {"NAME": "rings", "TYPE": "float", "DEFAULT": 5.0, "MIN": 1.0, "MAX": 6.0, "LABEL": "Number of rings"},
    {"NAME": "dots", "TYPE": "float", "DEFAULT": 6.0, "MIN": 3.0, "MAX": 12.0, "LABEL": "Dots on inner ring"},
    {"NAME": "dotSize", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.4, "MAX": 2.0, "LABEL": "Dot size"},
    {"NAME": "spacing", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.5, "MAX": 1.5, "LABEL": "Ring spacing"},
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
  float a = atan(p.y, p.x);
  float nr = floor(rings + 0.5);
  vec3 col = backColor.rgb;
  float cm = smoothstep(0.02 * dotSize, 0.012 * dotSize, length(p));
  col += dotColor.rgb * cm;
  for (int i = 0; i < 6; i++) {
    float fi = float(i);
    if (fi >= nr) break;
    float R = (0.07 + 0.065 * fi) * spacing;
    float n = floor(dots + 0.5) + 2.0 * fi;
    float step_ = TAU / n;
    float spin = t * 0.7 / (1.0 + 0.25 * fi) * (mod(fi, 2.0) * 2.0 - 1.0);
    float sector = floor((a - spin) / step_ + 0.5);
    float ang = sector * step_ + spin;
    vec2 pos = R * vec2(cos(ang), sin(ang));
    float rad = (0.012 + 0.003 * fi) * dotSize * (1.0 + 0.25 * sin(t * 2.0 + sector));
    float d = length(p - pos);
    vec3 c = mix(dotColor.rgb, accentColor.rgb, fi / max(nr - 1.0, 1.0));
    col += c * smoothstep(rad, rad * 0.55, d);
    col += c * 0.35 * exp(-d * d / (rad * rad * 5.0));
  }
  gl_FragColor = vec4(clamp(col, 0.0, 1.0), 1.0);
}
