/*{
  "DESCRIPTION": "Four soft colour blobs drifting and melting into each other, with optional swirl and film grain.",
  "CREDIT": "Built-in",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Gradient"],
  "INPUTS": [
    {"NAME": "color1", "TYPE": "color", "DEFAULT": [1.0, 0.42, 0.6, 1.0], "LABEL": "Colour 1"},
    {"NAME": "color2", "TYPE": "color", "DEFAULT": [0.36, 0.49, 1.0, 1.0], "LABEL": "Colour 2"},
    {"NAME": "color3", "TYPE": "color", "DEFAULT": [0.1, 0.83, 0.77, 1.0], "LABEL": "Colour 3"},
    {"NAME": "color4", "TYPE": "color", "DEFAULT": [1.0, 0.78, 0.34, 1.0], "LABEL": "Colour 4"},
    {"NAME": "distortion", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 1.0, "LABEL": "Distortion"},
    {"NAME": "swirl", "TYPE": "float", "DEFAULT": 0.4, "MIN": 0.0, "MAX": 1.0, "LABEL": "Swirl"},
    {"NAME": "grain", "TYPE": "float", "DEFAULT": 0.25, "MIN": 0.0, "MAX": 1.0, "LABEL": "Grain"},
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
  float asp = aspect();
  vec2 p = centred();
  float t = TIME * speed * 0.3;

  float r = length(p);
  p = rot(swirl * 2.5 * exp(-r * 2.2) * sin(t * 0.8 + 1.0)) * p;
  p += distortion * 0.22 * vec2(gnoise(p * 2.2 + vec2(t, 0.0)), gnoise(p * 2.2 + vec2(5.2, -t)));
  p += distortion * 0.05 * vec2(sin(p.y * 7.0 + t * 2.0), cos(p.x * 7.0 - t * 2.0));

  float wsum = 0.0;
  vec3 acc = vec3(0.0);
  for (int i = 0; i < 4; i++) {
    float fi = float(i);
    vec2 base = vec2(mod(fi, 2.0) * 2.0 - 1.0, floor(fi * 0.5) * 2.0 - 1.0) * vec2(0.4 * asp, 0.32);
    vec2 c = base + 0.2 * vec2(sin(t * (1.0 + 0.13 * fi) + fi * 1.7), cos(t * (0.8 + 0.11 * fi) + fi * 2.3));
    vec3 col = i == 0 ? color1.rgb : i == 1 ? color2.rgb : i == 2 ? color3.rgb : color4.rgb;
    vec2 d = p - c;
    float d2 = dot(d, d);
    float w = 1.0 / (d2 * d2 * 40.0 + d2 * 4.0 + 0.02);
    acc += col * w;
    wsum += w;
  }
  vec3 col = acc / wsum;
  col += (hash21(gl_FragCoord.xy) - 0.5) * grain * 0.3;
  gl_FragColor = vec4(clamp(col, 0.0, 1.0), 1.0);
}
