/*{
  "DESCRIPTION": "Diagonal stripes in two colours that slide along and can ripple like fabric.",
  "CREDIT": "Built-in",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Pattern"],
  "INPUTS": [
    {"NAME": "colorA", "TYPE": "color", "DEFAULT": [0.97, 0.93, 0.85, 1.0], "LABEL": "Stripe colour"},
    {"NAME": "colorB", "TYPE": "color", "DEFAULT": [0.9, 0.25, 0.3, 1.0], "LABEL": "Gap colour"},
    {"NAME": "count", "TYPE": "float", "DEFAULT": 8.0, "MIN": 2.0, "MAX": 40.0, "LABEL": "Number of stripes"},
    {"NAME": "angle", "TYPE": "float", "DEFAULT": 45.0, "MIN": 0.0, "MAX": 180.0, "LABEL": "Angle (degrees)"},
    {"NAME": "duty", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.1, "MAX": 0.9, "LABEL": "Stripe thickness"},
    {"NAME": "softness", "TYPE": "float", "DEFAULT": 0.05, "MIN": 0.005, "MAX": 0.5, "LABEL": "Edge softness"},
    {"NAME": "waviness", "TYPE": "float", "DEFAULT": 0.4, "MIN": 0.0, "MAX": 1.0, "LABEL": "Ripple"},
    {"NAME": "speed", "TYPE": "float", "DEFAULT": 0.5, "MIN": -2.0, "MAX": 2.0, "LABEL": "Speed (negative reverses)"}
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
  vec2 q = rot(radians(angle)) * centred();
  q.x += waviness * 0.08 * sin(q.y * 9.0 + t * 1.5);
  float s = fract(q.x * count + t * 0.5);
  float bar = smoothstep(0.0, softness, s) * (1.0 - smoothstep(duty, duty + softness, s));
  gl_FragColor = vec4(mix(colorB.rgb, colorA.rgb, bar), 1.0);
}
