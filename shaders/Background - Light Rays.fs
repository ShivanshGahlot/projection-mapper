/*{
  "DESCRIPTION": "Background behind the gallery: a brilliant hidden light behind the frames sends rays fanning out through every gap, slowly turning and breathing, with sparkling dust drifting through the beams. The frames become silhouettes edged with light. Edges fade softly and nothing is projected on the sofa.",
  "CREDIT": "Created for Ghost Arcade",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Audio Reactive", "Projection Mapping"],
  "INPUTS": [
    {"NAME": "speed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.25, "MAX": 3.0, "LABEL": "Speed"},
    {"NAME": "brightness", "TYPE": "float", "DEFAULT": 0.8, "MIN": 0.1, "MAX": 1.0, "LABEL": "Brightness"},
    {"NAME": "rayCount", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0, "LABEL": "Number of Rays"},
    {"NAME": "rayContrast", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 1.0, "LABEL": "Ray Sharpness"},
    {"NAME": "dustAmount", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 1.0, "LABEL": "Sparkling Dust"},
    {"NAME": "colorMode", "TYPE": "long", "DEFAULT": 0, "VALUES": [0, 1, 2], "LABELS": ["Golden", "Saffron Sunset", "Changing Over Time"], "LABEL": "Light Colour"},
    {"NAME": "centerX", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.2, "MAX": 0.8, "LABEL": "Light Source Across"},
    {"NAME": "centerY", "TYPE": "float", "DEFAULT": 0.42, "MIN": 0.2, "MAX": 0.8, "LABEL": "Light Source Down"},
    {"NAME": "audioDrive", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0, "LABEL": "Audio Drive"},
    {"NAME": "sofaLine", "TYPE": "float", "DEFAULT": 0.19, "MIN": 0.0, "MAX": 0.5, "LABEL": "Sofa Line (from bottom)"},
    {"NAME": "featherTop", "TYPE": "float", "DEFAULT": 0.12, "MIN": 0.0, "MAX": 0.4, "LABEL": "Fade Top Edge"},
    {"NAME": "featherSides", "TYPE": "float", "DEFAULT": 0.10, "MIN": 0.0, "MAX": 0.4, "LABEL": "Fade Side Edges"}
  ]
}*/

#ifdef GL_ES
precision highp float;
#endif

const float TAU = 6.28318530718;
const float PI = 3.14159265359;
const vec2 CANVAS = vec2(64.0, 42.0);

float hash21(vec2 p) {
  p = fract(p * vec2(123.34, 456.21));
  p += dot(p, p + 45.32);
  return fract(p.x * p.y);
}

float vnoise(vec2 p) {
  vec2 i = floor(p);
  vec2 f = fract(p);
  f = f * f * (3.0 - 2.0 * f);
  float a = hash21(i);
  float b = hash21(i + vec2(1.0, 0.0));
  float c = hash21(i + vec2(0.0, 1.0));
  float d = hash21(i + vec2(1.0, 1.0));
  return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float lineMask(float d, float w, float aa) {
  return 1.0 - smoothstep(w, w + aa, d);
}

float segDist(vec2 p, vec2 a, vec2 b, out float h) {
  vec2 pa = p - a;
  vec2 ba = b - a;
  h = clamp(dot(pa, ba) / max(dot(ba, ba), 0.0000001), 0.0, 1.0);
  return length(pa - ba * h);
}

// Soft fades at the projection edges and nothing projected on the sofa.
float wallMask(vec2 uv, vec2 w, float sofaY, float fTop, float fSides) {
  float m = 1.0 - smoothstep(sofaY - 0.4, sofaY + 0.4, w.y);
  m *= smoothstep(0.0, max(fSides * 0.5, 0.001), min(uv.x, 1.0 - uv.x));
  m *= smoothstep(0.0, max(fTop, 0.001), 1.0 - uv.y);
  return m;
}


vec3 rayPalette(int mode, float tm, float t) {
  vec3 a = vec3(1.0, 0.82, 0.45);
  vec3 b = vec3(1.0, 0.60, 0.22);
  if (mode == 1) { a = vec3(1.0, 0.62, 0.25); b = vec3(0.95, 0.30, 0.25); }
  if (mode == 2) {
    float ph = fract(tm / 40.0) * 3.0;
    vec3 c0 = vec3(1.0, 0.80, 0.42);
    vec3 c1 = vec3(1.0, 0.45, 0.55);
    vec3 c2 = vec3(0.70, 0.55, 1.0);
    vec3 cA = ph < 1.0 ? mix(c0, c1, smoothstep(0.6, 1.0, ph)) : (ph < 2.0 ? mix(c1, c2, smoothstep(1.6, 2.0, ph)) : mix(c2, c0, smoothstep(2.6, 3.0, ph)));
    a = cA;
    b = cA * vec3(1.0, 0.75, 0.6);
  }
  return mix(a, b, t);
}

void main() {
  vec2 uv = gl_FragCoord.xy / RENDERSIZE.xy;
  vec2 w = vec2(uv.x, 1.0 - uv.y) * CANVAS;
  float px = max(CANVAS.x / RENDERSIZE.x, CANVAS.y / RENDERSIZE.y);
  float tm = TIME * speed;
  float bright = brightness;
  float nR = rayCount;
  float sharp = rayContrast;
  float dust = dustAmount;
  int mode = int(colorMode);
  float sofaY = CANVAS.y * (1.0 - sofaLine);
  float bass = max(audioBass, 0.2 + 0.08 * sin(TIME * 0.7)) * audioDrive;

  vec2 cen = vec2(centerX, centerY) * CANVAS;
  vec2 d = w - cen;
  float r = length(d);
  vec2 dir = d / max(r, 0.001);

  // rays: noise around the circle (seamless), two layers turning in opposite directions
  float rot1 = tm * 0.02;
  float rot2 = -tm * 0.013;
  vec2 c1 = vec2(cos(atan(dir.y, dir.x) + rot1), sin(atan(dir.y, dir.x) + rot1));
  vec2 c2 = vec2(cos(atan(dir.y, dir.x) + rot2), sin(atan(dir.y, dir.x) + rot2));
  float freq = 3.0 + 6.0 * nR;
  float n1 = vnoise(c1 * freq + vec2(tm * 0.05, 3.0));
  float n2 = vnoise(c2 * freq * 1.7 + vec2(7.0, -tm * 0.04));
  float rays = n1 * 0.6 + n2 * 0.4;
  float lo = mix(0.25, 0.48, sharp);
  float hi = mix(0.85, 0.62, sharp);
  rays = smoothstep(lo, hi, rays);
  float breathe = 0.85 + 0.15 * sin(tm * 0.35) + 0.2 * bass;
  float fall = exp(-r / (26.0 * breathe)) * (1.0 - exp(-r / 2.5));
  vec3 rc = rayPalette(mode, tm, clamp(r / 40.0, 0.0, 1.0));
  vec3 col = rc * rays * fall * 1.1;
  col += rc * exp(-r / 9.0) * 0.45 * breathe;
  col += vec3(1.0, 0.95, 0.85) * exp(-r / 3.5) * 0.6;

  // sparkling dust drifting through the beams
  vec2 dg = w * 1.4 + vec2(tm * 0.25, -tm * 0.15);
  vec2 did = floor(dg);
  float dh = hash21(did + 13.0);
  vec2 doff = vec2(hash21(did + 3.0), hash21(did + 7.0)) - 0.5;
  float dd = length(fract(dg) - 0.5 - doff * 0.6);
  float tw = 0.5 + 0.5 * sin(tm * 3.0 + dh * 50.0);
  float mote = step(1.0 - 0.10 * dust, dh) * (lineMask(dd, 0.06, 0.08) + exp(-dd / 0.12) * 0.3);
  col += mix(vec3(1.0), rc, 0.4) * mote * tw * (0.25 + 1.6 * rays * fall + exp(-r / 10.0));

  col *= bright * wallMask(uv, w, sofaY, featherTop, featherSides);
  gl_FragColor = vec4(clamp(col, 0.0, 1.0), 1.0);
}
