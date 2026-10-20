/*{
  "DESCRIPTION": "A three-colour glow that radiates from a point, gently breathing and drifting.",
  "CREDIT": "Built-in",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Gradient"],
  "INPUTS": [
    {"NAME": "inner", "TYPE": "color", "DEFAULT": [1.0, 0.93, 0.7, 1.0], "LABEL": "Centre colour"},
    {"NAME": "middle", "TYPE": "color", "DEFAULT": [1.0, 0.3, 0.45, 1.0], "LABEL": "Middle colour"},
    {"NAME": "outer", "TYPE": "color", "DEFAULT": [0.12, 0.05, 0.3, 1.0], "LABEL": "Edge colour"},
    {"NAME": "centerX", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0, "LABEL": "Centre across"},
    {"NAME": "centerY", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0, "LABEL": "Centre up"},
    {"NAME": "size", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.1, "MAX": 1.5, "LABEL": "Size"},
    {"NAME": "wobble", "TYPE": "float", "DEFAULT": 0.3, "MIN": 0.0, "MAX": 1.0, "LABEL": "Wobbly edge"},
    {"NAME": "pulse", "TYPE": "float", "DEFAULT": 0.3, "MIN": 0.0, "MAX": 1.0, "LABEL": "Breathing"},
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

void main() {
  float t = TIME * speed;
  vec2 c = (vec2(centerX, centerY) - 0.5) * vec2(aspect(), 1.0);
  c += 0.03 * vec2(sin(t * 0.7), cos(t * 0.9));
  vec2 p = centred() - c;
  float a = atan(p.y, p.x);
  float r = length(p);
  float breath = 1.0 + pulse * 0.25 * sin(t * 1.6);
  r /= size * breath;
  r *= 1.0 + wobble * 0.2 * (sin(a * 3.0 + t * 1.3) * 0.6 + sin(a * 5.0 - t * 0.9) * 0.4);
  vec3 col = mix(inner.rgb, middle.rgb, smoothstep(0.0, 0.55, r));
  col = mix(col, outer.rgb, smoothstep(0.35, 1.1, r));
  gl_FragColor = vec4(col, 1.0);
}
