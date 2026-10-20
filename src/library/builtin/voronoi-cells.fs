/*{
  "DESCRIPTION": "A mosaic of drifting cells, each tinted between two colours and outlined with glowing edges.",
  "CREDIT": "Built-in",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Noise"],
  "INPUTS": [
    {"NAME": "colorA", "TYPE": "color", "DEFAULT": [0.1, 0.25, 0.6, 1.0], "LABEL": "Cell colour 1"},
    {"NAME": "colorB", "TYPE": "color", "DEFAULT": [0.95, 0.4, 0.55, 1.0], "LABEL": "Cell colour 2"},
    {"NAME": "edgeColor", "TYPE": "color", "DEFAULT": [1.0, 0.95, 0.8, 1.0], "LABEL": "Edge colour"},
    {"NAME": "scale", "TYPE": "float", "DEFAULT": 5.0, "MIN": 2.0, "MAX": 20.0, "LABEL": "Number of cells"},
    {"NAME": "edgeWidth", "TYPE": "float", "DEFAULT": 0.08, "MIN": 0.0, "MAX": 0.3, "LABEL": "Edge width"},
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
  vec2 g = centred() * scale + vec2(t * 0.1, 0.0);
  vec2 ip = floor(g);
  vec2 fp = fract(g);
  float d1 = 8.0;
  float d2 = 8.0;
  vec2 id = ip;
  for (int j = -1; j <= 1; j++) {
    for (int i = -1; i <= 1; i++) {
      vec2 cell = vec2(float(i), float(j));
      vec2 h = vec2(hash21(ip + cell), hash21(ip + cell + 17.3));
      vec2 o = 0.5 + 0.42 * sin(t + TAU * h);
      float d = length(cell + o - fp);
      if (d < d1) {
        d2 = d1;
        d1 = d;
        id = ip + cell;
      } else if (d < d2) {
        d2 = d;
      }
    }
  }
  float tint = hash21(id + 3.7);
  vec3 col = mix(colorA.rgb, colorB.rgb, tint) * (1.0 - 0.45 * d1);
  float edge = 1.0 - smoothstep(0.0, edgeWidth + 0.001, d2 - d1);
  col = mix(col, edgeColor.rgb, edge * step(0.001, edgeWidth));
  gl_FragColor = vec4(col, 1.0);
}
