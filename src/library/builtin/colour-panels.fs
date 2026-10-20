/*{
  "DESCRIPTION": "Bold colour panels sliding past each other, each shading into the next colour.",
  "CREDIT": "Built-in",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Pattern"],
  "INPUTS": [
    {"NAME": "color1", "TYPE": "color", "DEFAULT": [0.95, 0.3, 0.35, 1.0], "LABEL": "Colour 1"},
    {"NAME": "color2", "TYPE": "color", "DEFAULT": [0.98, 0.75, 0.25, 1.0], "LABEL": "Colour 2"},
    {"NAME": "color3", "TYPE": "color", "DEFAULT": [0.15, 0.6, 0.65, 1.0], "LABEL": "Colour 3"},
    {"NAME": "color4", "TYPE": "color", "DEFAULT": [0.2, 0.2, 0.55, 1.0], "LABEL": "Colour 4"},
    {"NAME": "panels", "TYPE": "float", "DEFAULT": 5.0, "MIN": 1.0, "MAX": 16.0, "LABEL": "Panels across"},
    {"NAME": "angle", "TYPE": "float", "DEFAULT": 20.0, "MIN": 0.0, "MAX": 180.0, "LABEL": "Tilt (degrees)"},
    {"NAME": "shading", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 1.0, "LABEL": "Shading into next colour"},
    {"NAME": "gap", "TYPE": "float", "DEFAULT": 0.03, "MIN": 0.0, "MAX": 0.2, "LABEL": "Gap between panels"},
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

vec3 pick(float k) {
  k = mod(k, 4.0);
  if (k < 1.0) return color1.rgb;
  if (k < 2.0) return color2.rgb;
  if (k < 3.0) return color3.rgb;
  return color4.rgb;
}

void main() {
  vec2 q = rot(radians(angle)) * centred();
  float x = (q.x + 0.5) * panels + TIME * speed * 0.4;
  float id = floor(x);
  float f = fract(x);
  float v = q.y + 0.5 + 0.35 * sin(id * 1.7 + TIME * speed * 0.3);
  vec3 col = mix(pick(id), pick(id + 1.0), clamp(v, 0.0, 1.0) * shading);
  col *= 0.88 + 0.12 * f;
  float edge = min(f, 1.0 - f);
  col = mix(vec3(0.03), col, smoothstep(gap * 0.5, gap * 0.5 + 0.01, edge) * step(0.0005, gap) + (1.0 - step(0.0005, gap)));
  gl_FragColor = vec4(col, 1.0);
}
