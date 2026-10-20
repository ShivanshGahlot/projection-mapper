/*{
  "DESCRIPTION": "Soft layered clouds drifting across the sky, lit from one side.",
  "CREDIT": "Built-in",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Noise"],
  "INPUTS": [
    {"NAME": "skyColor", "TYPE": "color", "DEFAULT": [0.18, 0.45, 0.85, 1.0], "LABEL": "Sky colour"},
    {"NAME": "cloudColor", "TYPE": "color", "DEFAULT": [1.0, 1.0, 1.0, 1.0], "LABEL": "Cloud colour"},
    {"NAME": "shadeColor", "TYPE": "color", "DEFAULT": [0.45, 0.52, 0.7, 1.0], "LABEL": "Cloud shadow colour"},
    {"NAME": "coverage", "TYPE": "float", "DEFAULT": 0.55, "MIN": 0.0, "MAX": 1.0, "LABEL": "Cloud cover"},
    {"NAME": "scale", "TYPE": "float", "DEFAULT": 2.0, "MIN": 0.5, "MAX": 6.0, "LABEL": "Cloud detail (higher = smaller clouds)"},
    {"NAME": "speed", "TYPE": "float", "DEFAULT": 0.4, "MIN": 0.0, "MAX": 2.0, "LABEL": "Wind speed"}
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

float clouds(vec2 p) {
  float s = 0.0;
  float a = 0.5;
  for (int i = 0; i < 5; i++) {
    s += a * gnoise(p);
    p = p * 2.03 + vec2(7.3, 1.9);
    a *= 0.5;
  }
  return 0.5 + 0.5 * s / 0.97;
}

void main() {
  float t = TIME * speed * 0.15;
  vec2 uv = gl_FragCoord.xy / RENDERSIZE.xy;
  vec2 p = centred() * scale + vec2(t, t * 0.2);
  float f = clouds(p);
  float f2 = clouds(p + vec2(-0.06, 0.09));
  float thr = mix(0.78, 0.35, coverage);
  float d = smoothstep(thr, thr + 0.22, f);
  float light = clamp(0.55 + (f - f2) * 9.0, 0.0, 1.0);
  vec3 sky = skyColor.rgb * (0.85 + 0.25 * uv.y);
  vec3 cloud = mix(shadeColor.rgb, cloudColor.rgb, light);
  gl_FragColor = vec4(mix(sky, cloud, d), 1.0);
}
